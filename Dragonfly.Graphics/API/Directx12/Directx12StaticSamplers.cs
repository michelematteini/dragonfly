using Dragonfly.Graphics.API.Common;
using Dragonfly.Graphics.Shaders;
using DragonflyGraphicsWrappers.DX12;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Dragonfly.Graphics.API.Directx12
{
    /// <summary>
    /// Track and describe all the static samplers added to the root signature.
    /// </summary>
    internal class Directx12StaticSamplers
    {
        public const int COUNT = 36; // total count of static samplers

        public static string GetSamplerName(int address)
        {
            return "_staticSampler" + address;
        }

        private Dictionary<TextureBindingOptions, int> samplerRegisters;
        private List<TextureBindingOptions> sortedSamplers;

        public Directx12StaticSamplers()
        {
            // generate all static sampler combinations
            {
                sortedSamplers = new List<TextureBindingOptions>(COUNT);
                foreach (TextureBindingOptions adressing in GetAddressModes())
                {
                    foreach (TextureBindingOptions filter in GetFilteringModes())
                    {
                        sortedSamplers.Add(adressing | filter | TextureBindingOptions.NoMipMaps);
                        sortedSamplers.Add(adressing | filter | TextureBindingOptions.MipMaps);
                    }
                }

                if (sortedSamplers.Count != COUNT)
                    throw new Exception("The exposed COUNT and the generated samplers count should match! If not update Directx12StaticSamplers.COUNT.");

                // fill a lookup table for the registers
                samplerRegisters = new Dictionary<TextureBindingOptions, int>();
                for (int i = 0; i < COUNT; i++)
                    samplerRegisters[sortedSamplers[i]] = i;
            }       

        }

        private IEnumerable<TextureBindingOptions> GetAddressModes()
        {
            yield return TextureBindingOptions.Wrap;
            yield return TextureBindingOptions.Mirror;
            yield return TextureBindingOptions.Clamp;
            yield return TextureBindingOptions.BorderBlack;
            yield return TextureBindingOptions.BorderWhite;
            yield return TextureBindingOptions.BorderTransparent;
        }

        private IEnumerable<TextureBindingOptions> GetFilteringModes()
        {
            yield return TextureBindingOptions.LinearFilter;
            yield return TextureBindingOptions.NoFilter;
            yield return TextureBindingOptions.Anisotropic;
        }

        public string GetSamplerName(TextureBindingOptions tbo)
        {
            return GetSamplerName(samplerRegisters[tbo & TextureBindingOptions.SamplerOptions]);
        }

        public DF_SamplerDesc12[] ToOptionsArray()
        {

            DF_SamplerDesc12[] samplerDescList = new DF_SamplerDesc12[COUNT];

            for (int i = 0; i < COUNT; i++)
            {
                TextureBindingOptions address = sortedSamplers[i] & TextureBindingOptions.Coords;
                samplerDescList[i].AddressType = DirectxUtils.AddressBindToDX(address);
                samplerDescList[i].BorderColor = DF_StaticBorderColor12.TransparentBlack;
                if (address == TextureBindingOptions.BorderWhite)
                    samplerDescList[i].BorderColor = DF_StaticBorderColor12.OpaqueWhite;
                else if (address == TextureBindingOptions.BorderBlack)
                    samplerDescList[i].BorderColor = DF_StaticBorderColor12.OpaqueBlack;
                TextureBindingOptions filter = sortedSamplers[i] & TextureBindingOptions.Filter;
                samplerDescList[i].Filter = DF_TextureFilterType12.Linear;
                if(filter == TextureBindingOptions.NoFilter)
                        samplerDescList[i].Filter = DF_TextureFilterType12.MinMagPointMipLinear;
                else if (filter == TextureBindingOptions.Anisotropic)
                    samplerDescList[i].Filter = DF_TextureFilterType12.Anisotropic;
                samplerDescList[i].MipMaps = (sortedSamplers[i] & TextureBindingOptions.MipMapMode) == TextureBindingOptions.MipMaps;
            }

            return samplerDescList;
        }

    }
}
