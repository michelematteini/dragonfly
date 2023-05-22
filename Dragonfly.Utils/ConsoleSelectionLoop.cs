using System;
using System.Collections.Generic;

namespace Dragonfly.Utils
{
    public class ConsoleSelectionLoop
    {
        private List<IConsoleProgram> programs;
        private string selectionTitle;

        public ConsoleSelectionLoop(string title)
        {
            programs = new List<IConsoleProgram>();
            selectionTitle = title;
            ManageExceptions = true;
        }

        public bool ManageExceptions { get; set; }

        public void AddProgram(IConsoleProgram program)
        {
            programs.Add(program);
        }

        public void Start()
        {
            Console.WriteLine("============================================");
            Console.WriteLine(selectionTitle);

            int choiceNum;
            bool quitApplication = false;

            while (!quitApplication)
            {
                Console.WriteLine("============================================");
                Console.WriteLine();
                for (int i = 0; i < programs.Count; i++)
                {
                    Console.WriteLine(string.Format("{0}- {1}", i, programs[i].ProgramName));
                }

#if DEBUG
                Console.WriteLine("E- Enable / Disable Exception management.");
#endif

                Console.WriteLine();
                Console.WriteLine("Choose a program: ");

                choiceNum = -1;
                bool programSelected = false;
                while (!programSelected)
                {
                    string choice = Console.ReadLine();
                    programSelected = true;
#if DEBUG
                    if (choice.ToUpper() == "E")
                    {
                        programSelected = false;
                        ManageExceptions = !ManageExceptions;
                        Console.WriteLine(string.Format("Exception management {0}.", ManageExceptions ? "enabled" : "disabled"));
                        Console.WriteLine("Choose a test: ");
                    }
                    else
                    {
#endif
                        if (!int.TryParse(choice, out choiceNum))
                        {
                            programSelected = false;
                            Console.WriteLine("Insert only the number of the choosen test:");
                        }
                        else if (choiceNum < 0)
                        {
                            programSelected = false;
                            Console.WriteLine("Negative numbers are invalid, choose a valid test:");
                        }
                        else if (choiceNum >= programs.Count)
                        {
                            programSelected = false;
                            Console.WriteLine(string.Format("Program #{0} not available, choose a valid test:", choiceNum));
                        }
#if DEBUG
                    }
#endif
                }
                Console.WriteLine();

                if (ManageExceptions)
                {
                    try
                    {
                        programs[choiceNum].RunProgram();
                    }
                    catch (Exception e)
                    {
                        Console.WriteLine(e.ToString());
                        Console.Write(e.StackTrace);
                        Console.WriteLine();
                    }
                }
                else
                {
                    programs[choiceNum].RunProgram();
                }
            }

        }
    }

    public interface IConsoleProgram
    {
        string ProgramName { get; }

        void RunProgram();
    }

    /// <summary>
    /// Concatenates multiple console programs into one.
    /// </summary>
    public class ConsoleProgramCat : IConsoleProgram
    {
        public List<IConsoleProgram> Programs { get; private set; }

        public string ProgramName { get; private set; }

        public ConsoleProgramCat(string name, params IConsoleProgram[] programs)
        {
            ProgramName = name;
            Programs = new List<IConsoleProgram>();
            Programs.AddRange(programs);
        }

        public void RunProgram()
        {
            foreach (IConsoleProgram program in Programs)
                program.RunProgram();
        }
    }
}
