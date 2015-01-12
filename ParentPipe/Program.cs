using System;
using System.IO.Pipes; // Pipes are contained in here
using System.Diagnostics; // Required for this process to start other processes
using System.IO; // Required for handle, so the child can inherate the handle
using System.Threading; // Required for thread sleeping

namespace ParentPipe
{
    class Program
    {
        private static readonly int PROCESSES_TOTAL = 50;
        private static readonly int PROCESSES_MADE = 5;

        static void Main(string[] args)
        {
            String childDebug = "false";
            Process[] pipeChilds;
            int totalLoops = 1;
            int waitTime = 1000; // How long it'll be before it prints off how much work has been done, in milliseconds
            int loopCount = 0;
            StreamReader sr;
            StreamWriter sw;
            String input;
            int workDone = 0;
            int createdChildren = 0;

            // Send debug console output from the client process.
            Console.Write("[PARENT] Child debug Mode (true,false): ");
            char tempDebug = Console.ReadLine().ToLower().ToCharArray()[0];

            if (tempDebug == 't')
            {
                childDebug = "true";
            }

            // How long the program will wait before resetting work count.
            try
            {
                Console.Write("[PARENT] How long do you want the program to wait in seconds (1, 2, 3...): ");
                waitTime = Convert.ToInt32(Console.ReadLine()) * 1000;
            }
            catch (FormatException e)
            {
                Console.WriteLine("Error: {0}", e.Message);
                waitTime = 1000;
            }

            // How many times the program will loop.
            try
            {
                Console.Write("[PARENT] How many times will it loop (1, 2, 3...): ");
                totalLoops = Convert.ToInt32(Console.ReadLine());
            }
            catch (FormatException e)
            {
                Console.WriteLine("Error: {0}", e.Message);
                totalLoops = 1;
            }

            // Array of child processes, none are made yet
            pipeChilds = new Process[PROCESSES_TOTAL];

            // Array of out pipes for the child processes, none are made yet
            AnonymousPipeServerStream[] pipeServerOut = new AnonymousPipeServerStream[PROCESSES_TOTAL];

            // Array of in pipes for the child processes, none are made yet
            AnonymousPipeServerStream[] pipeServerIn = new AnonymousPipeServerStream[PROCESSES_TOTAL];

            // Creating the in and out pipes for the child processes.
            for (int i = 0; i < PROCESSES_TOTAL; i++)
            {
                pipeServerIn[i] = new AnonymousPipeServerStream(PipeDirection.In, HandleInheritability.Inheritable);
                pipeServerOut[i] = new AnonymousPipeServerStream(PipeDirection.Out, HandleInheritability.Inheritable);
            }

            // Start of the monitor loop
            while (loopCount < totalLoops)
            {
                loopCount++;

                // Starting up additional processes,  will be maded on the PROCESSES_TOTAL
                if (createdChildren < PROCESSES_TOTAL)
                {
                    for (int i = createdChildren; i < (createdChildren + PROCESSES_MADE); i++)
                    {
                        // Creating new child process
                        pipeChilds[i] = new Process();

                        // This tell the program that the file is called ChildPipe.exe
                        pipeChilds[i].StartInfo.FileName = "ChildPipe.exe";

                        // This sets the clients main args[]
                        pipeChilds[i].StartInfo.Arguments =
                            pipeServerIn[i].GetClientHandleAsString() + " " +
                            pipeServerOut[i].GetClientHandleAsString() + " " +
                            (i + 1) + " " +
                            childDebug;

                        // Just tells the program if it's going to ask the OS to open a new shell or not.
                        pipeChilds[i].StartInfo.UseShellExecute = false;

                        // Starting the program.
                        pipeChilds[i].Start();
                    }
                    createdChildren = createdChildren + PROCESSES_MADE;
                }

                // Telling the thread to sleep, giving the child processes some time to work.
                Thread.Sleep(waitTime);

                // Telling all the processes to send the new work done.
                for (int i = 0; i < createdChildren; i++)
                {
                    // StreamWriter is getting the out pipes.
                    sw = new StreamWriter(pipeServerOut[i]);
                    sw.AutoFlush = true;

                    // This writes to the pipe.
                    sw.WriteLine("Update");
                }

                // Reading the results back.
                for (int i = 0; i < createdChildren; i++)
                {
                    // StreamReader is getting the in pipes.
                    sr = new StreamReader(pipeServerIn[i]);

                    // Reading the in pipe, will block if there is nothing in there.
                    input = sr.ReadLine();

                    if (input != "")
                        try
                        {
                            // Since I'm using StreamReader, it needs to be converted from String to int.
                            workDone = workDone + Convert.ToInt32(input);
                        }
                        catch (FormatException e)
                        {
                            Console.WriteLine("Error: {0}", e.Message);
                        }
                }

                // Writting the work done to the console window.
                Console.WriteLine("Work done since last update (" + createdChildren + " Processes): " + workDone);

                // Resetting the workDone so it'll be ready for the next update.
                workDone = 0;
            }

            // Telling all the processes to stop all work and close.
            for (int i = 0; i < pipeServerOut.Length; i++)
            {
                using (sw = new StreamWriter(pipeServerOut[i]))
                {
                    // Telling each child process to stop work and shut down.
                    sw.WriteLine("Stop");
                } // Will close off the Writer.

                // Closing off the Readers
                sr = new StreamReader(pipeServerIn[i]);
                sr.Close();
            }

            // Writing to the console that the program is finished.
            Console.WriteLine("[SERVER] Finished with program...");
            Console.ReadLine();
        }
    }
}