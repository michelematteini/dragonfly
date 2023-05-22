using System.Linq;
using System.Collections.Generic;
using System.IO;
using Dragonfly.Utils;
using System;

namespace Dragonfly.Graphics.Shaders
{
    public interface IBindingTable
    {
        bool ContainsInput(string shaderName, string name);

        InputBinding GetInput(string shaderName, string name);

        bool ContainsEffectInput(string effectName, string name);

        InputBinding GetEffectInput(string effectName, string name);

        EffectBinding GetEffect(string effectName, string templateName = "", string variantID = "");
    }

    public interface IEffectBinder
    {
        void BindEffect(EffectBinding binding);
    }

    public interface IInputBinder
    {
        void BindInput(InputBinding binding);
    }

    public class EffectTemplateRecord
    {
        public string TemplateName;
        public Dictionary<string, EffectBinding> VariantBindings; // [variantID] -> EffectBinding

        public EffectTemplateRecord(string templateName)
        {
            TemplateName = templateName;
            VariantBindings = new Dictionary<string, EffectBinding>();
        }
    }


    public class EffectBindingRecord
    { 
        public string ShaderName;
        public Dictionary<string, EffectTemplateRecord> Templates; // [template name] -> Effect variants

        public EffectBindingRecord(string shaderName)
        {
            ShaderName = shaderName;
            Templates = new Dictionary<string, EffectTemplateRecord>();
        }
    }

    public class InputBindingRecord
    {
        public Dictionary<string, InputBinding> Bindings;
        public HashSet<string> Variants;

        public InputBindingRecord()
        {
            Bindings = new Dictionary<string, InputBinding>();
            Variants = new HashSet<string>();
        }
    }

    public class ShaderBindingTable : IBindingTable, IEffectBinder, IInputBinder
    {
        public string Version { get; set; }

        private Dictionary<string, InputBindingRecord> inputs; // [shaderName] -> InputBinding
        private Dictionary<string, EffectBindingRecord> effects; // [effectName] -> EffectBinding
        private Dictionary<string, HashSet<string>> variants; // [variant name] -> value list
        private Dictionary<string, byte[]> programs; // all compiled programs, listed by their names

        public ShaderBindingTable()
        {
            Version = "0.3";
            inputs = new Dictionary<string, InputBindingRecord>();
            effects = new Dictionary<string, EffectBindingRecord>();
            variants = new Dictionary<string, HashSet<string>>();
            programs = new Dictionary<string, byte[]>();
        }

        public ShaderBindingTable(IGraphicsAPI api, byte[] serialized) : this()
        {
            ShaderCompiler binder = api.CreateShaderCompiler();
            MemoryStream stream = new MemoryStream(serialized);
            BinaryReader reader = new BinaryReader(stream);
            string tableName = reader.ReadString();
            string version = reader.ReadString();

            // load inputs
            inputs = SerializationUtils.ReadDictionary<InputBindingRecord>(reader, () =>
            {
                InputBindingRecord record = new InputBindingRecord();
                record.Bindings = SerializationUtils.ReadDictionary(reader, () => binder.CreateBindingFromStream((ShaderBindingType)reader.ReadInt32(), reader));
                record.Variants = SerializationUtils.ReadSet(reader, () => reader.ReadString());
                return record;
            });

            // load effects
            effects = SerializationUtils.ReadDictionary<EffectBindingRecord>(reader, () =>
            {
                EffectBindingRecord record = new EffectBindingRecord(reader.ReadString());
                record.Templates = SerializationUtils.ReadDictionary(reader, () =>
                {
                    EffectTemplateRecord templates = new EffectTemplateRecord(reader.ReadString());
                    templates.VariantBindings = SerializationUtils.ReadDictionary(reader, () => EffectBinding.FromStream(reader));
                    return templates;
                });
                return record;
            });

            // load global variant list
            variants = SerializationUtils.ReadDictionary(reader, () => SerializationUtils.ReadSet(reader, () => reader.ReadString()));

            // load programs
            programs = SerializationUtils.ReadDictionary(reader, () => SerializationUtils.ReadBytes(reader));

            reader.Close();
        }

        public InputBinding GetInput(string shaderName, string name)
        {
            return inputs[shaderName].Bindings[name];
        }

        public InputBinding GetEffectInput(string effectName, string name)
        {
            return inputs[effects[effectName].ShaderName].Bindings[name];
        }

        public HashSet<string> GetEffectVariantNames(string effectName)
        {
            return inputs[effects[effectName].ShaderName].Variants;
        }

        public string GetEffectDefaultVariantId(string effectName, string templateName)
        {
            EffectBindingRecord eRecord = effects[effectName];
            EffectTemplateRecord eTemplate = eRecord.Templates[templateName];
            return eTemplate.VariantBindings.First().Key;
        }

        public ICollection<string> GetEffectVariantIDs(string effectName)
        {
            return effects[effectName].Templates.Keys;
        }

        public string GetEffectDefaultTemplate(string effectName)
        {
            EffectBindingRecord eRecord = effects[effectName];
            return eRecord.Templates.First().Key;
        }

        public EffectBinding GetEffect(string effectName, string templateName = "", string variantID = "")
        {
            if (string.IsNullOrEmpty(templateName))
                templateName = GetEffectDefaultTemplate(effectName);

            if (string.IsNullOrEmpty(variantID)) 
                variantID = GetEffectDefaultVariantId(effectName, templateName);

            return effects[effectName].Templates[templateName].VariantBindings[variantID];
        }

        public List<string> GetAllShaderNames()
        {
            return inputs.Keys.ToList();
        }

        public string GetParentShaderName(string effectName)
        {
            return effects[effectName].ShaderName;
        }

        public IEnumerable<InputBinding> GetAllShaderInputs(string shaderName)
        {
            return inputs[shaderName].Bindings.Values;
        }

        public byte[] GetProgram(string programName)
        {
            return programs[programName];
        }

        public bool TryGetProgram(string programName, out byte[] byteCode)
        {
            return programs.TryGetValue(programName, out byteCode);
        }

        public bool ContainsProgram(string programName)
        {
            return programs.ContainsKey(programName);
        }

        public IEnumerable<string> GetVariantNameList()
        {
            return variants.Keys;
        }

        public IEnumerable<string> GetVariantValidValues(string variantName)
        {
            return variants[variantName];
        }

        public bool IsValidVariantValue(string variantName, string value)
        {
            return variants[variantName].Contains(value);
        }

        public bool IsValidVariantValue(string variantName, bool value)
        {
            return variants[variantName].Contains(value.ToString());
        }

        public bool ContainsInput(string shaderName, string name)
        {
            return inputs.ContainsKey(shaderName) && inputs[shaderName].Bindings.ContainsKey(name);
        }

        public bool ContainsEffectInput(string effectName, string name)
        {
            return ContainsInput(effects[effectName].ShaderName, name);
        }

        public IReadOnlyCollection<string> GetAllTemplatesFor(string effectName)
        {
            return effects[effectName].Templates.Keys;
        }

        public void BindEffect(EffectBinding binding)
        {
            EffectBindingRecord eRecord;

            if (!effects.TryGetValue(binding.EffectName, out eRecord))
            {
                eRecord = new EffectBindingRecord(binding.ShaderName);
                effects.Add(binding.EffectName, eRecord);
            }

            EffectTemplateRecord eTemplate;
            if(!eRecord.Templates.TryGetValue(binding.Template, out eTemplate))
            {
                eTemplate = new EffectTemplateRecord(binding.Template);
                eRecord.Templates.Add(binding.Template, eTemplate);
            }

            eTemplate.VariantBindings.Add(ShaderCompiler.GetShaderVariantID(binding.VariantValues), binding);
        }

        public void BindVariants(ShaderSrcFile fromShader)
        {
            if (!inputs.ContainsKey(fromShader.Name))
                inputs[fromShader.Name] = new InputBindingRecord();

            foreach (ShaderSrcFile.Variant v in fromShader.Variants)
            {
                // save that this shader use the named variant
                inputs[fromShader.Name].Variants.Add(v.Name);

                // save values for this variant
                HashSet<string> varValues = new HashSet<string>();
                foreach (ShaderSrcFile.VariantValue varVal in v.Values)
                    varValues.Add(varVal.Name);
                variants[v.Name] = varValues;
            }
        }

        public void BindInput(InputBinding binding)
        {
            if (!inputs.ContainsKey(binding.ShaderName))
                inputs[binding.ShaderName] = new InputBindingRecord();

            inputs[binding.ShaderName].Bindings[binding.Name] = binding;
        }

        public void BindProgram(string programName, byte[] programBinary)
        {
            programs[programName] = programBinary;
        }

        public byte[] ToByteArray()
        {
            MemoryStream stream = new MemoryStream();
            BinaryWriter writer = new BinaryWriter(stream);
            writer.Write(this.ToString());
            writer.Write(Version);

            // save inputs
            SerializationUtils.WriteDictionary(inputs, writer, inRecord =>
            {   
                // save bindings
                SerializationUtils.WriteDictionary(inRecord.Bindings, writer, inBinding => 
                {
                    writer.Write((int)inBinding.Type);
                    inBinding.Save(writer);
                }); 

                // save variants
                SerializationUtils.WriteSet(inRecord.Variants, writer, varName => writer.Write(varName));
            });

            // save effects
            SerializationUtils.WriteDictionary(effects, writer, eRecord =>
            {
                writer.Write(eRecord.ShaderName);
                SerializationUtils.WriteDictionary(eRecord.Templates, writer, eTemplate =>
                {
                    writer.Write(eTemplate.TemplateName);
                    SerializationUtils.WriteDictionary(eTemplate.VariantBindings, writer, eBinding => eBinding.Save(writer)); // save effect bindings
                });
            });

            // save global variant list
            SerializationUtils.WriteDictionary(variants, writer, varValues =>
            {  
                // save variant value list
                SerializationUtils.WriteSet(varValues, writer, varVal => writer.Write(varVal)); 
            });

            // save programs
            SerializationUtils.WriteDictionary(programs, writer, prog => SerializationUtils.WriteBytes(prog, writer));

            // convert to byte and return
            byte[] result = new byte[stream.Length];
            stream.Seek(0, SeekOrigin.Begin);
            stream.Read(result, 0, (int)stream.Length);
            writer.Close();
            return result;
        }

    }

}
