using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading;
using Google.Protobuf;
using KRPC.Conformance;

namespace KRPC.Client.Conformance
{
    /// <summary>
    /// The conformance testee for the C# client. Reads commands from the conformance driver
    /// on standard input, runs them against the client, and writes the replies to standard
    /// output. See conformance/testee.proto for the protocol.
    /// </summary>
    static class Program
    {
        static readonly string[] Profiles = {
            "core", "error-types", "defaults", "streams", "events", "functions", "function-types",
            "connections", "threads", "callbacks", "conditions", "ordering", "races"
        };

        static readonly string[] Protocols = { "tcpip", "localsocket" };

        static int Main ()
        {
            var output = new Writer (Console.OpenStandardOutput ());
            // The protocol owns standard output
            Console.SetOut (Console.Error);
            var hello = new Hello { Client = "csharp" };
            hello.Profiles.AddRange (Profiles);
            hello.Protocols.AddRange (Protocols);
            output.Write (new Output { Hello = hello });

            Testee.InstallWriteFault ();
            var testee = new Testee (output);
            var workers = new Dictionary<string, Worker> ();
            var input = new StreamReader (Console.OpenStandardInput (), new UTF8Encoding (false));
            while (true) {
                // Each command is its JSON form on a line of its own
                var line = input.ReadLine ();
                if (line == null)
                    break;
                Command command;
                try {
                    command = JsonParser.Default.Parse<Command> (line);
                } catch (InvalidJsonException) {
                    break;
                } catch (InvalidProtocolBufferException) {
                    break;
                }
                Worker worker;
                if (command.CommandCase == Command.CommandOneofCase.Interrupt) {
                    // Run here, as the worker to interrupt is busy
                    if (workers.TryGetValue (command.Interrupt.Thread, out worker))
                        worker.Interrupt ();
                    output.Write (new Output { Reply = new Reply { Id = command.Id, Value = new Value () } });
                    continue;
                }
                if (!workers.TryGetValue (command.Thread, out worker)) {
                    worker = new Worker (testee.Run);
                    workers [command.Thread] = worker;
                }
                worker.Submit (command);
            }
            foreach (var worker in workers.Values)
                worker.Stop ();
            testee.Close ();
            return 0;
        }
    }

    /// <summary>
    /// Writes messages to the driver. Replies and callbacks come from several threads, so each
    /// message is written whole under a lock.
    /// </summary>
    sealed class Writer
    {
        readonly Stream stream;
        readonly object writeLock = new object ();

        public Writer (Stream stream)
        {
            this.stream = stream;
        }

        public void Write (Output message)
        {
            var line = Encoding.UTF8.GetBytes (JsonFormatter.Default.Format (message) + "\n");
            lock (writeLock) {
                stream.Write (line, 0, line.Length);
                stream.Flush ();
            }
        }
    }

    /// <summary>
    /// A named thread that runs the commands sent to it in order.
    /// </summary>
    sealed class Worker
    {
        readonly BlockingCollection<Command> queue = new BlockingCollection<Command> ();
        readonly Thread thread;

        public Worker (Action<Command> run)
        {
            thread = new Thread (() => {
                while (true) {
                    Command command;
                    try {
                        if (!queue.TryTake (out command, Timeout.Infinite))
                            return;
                    } catch (ThreadInterruptedException) {
                        // An interrupt that arrived once the command it was for had finished
                        continue;
                    } catch (InvalidOperationException) {
                        return;
                    }
                    run (command);
                }
            });
            thread.IsBackground = true;
            thread.Start ();
        }

        public void Submit (Command command)
        {
            queue.Add (command);
        }

        public void Interrupt ()
        {
            thread.Interrupt ();
        }

        public void Stop ()
        {
            queue.CompleteAdding ();
            thread.Join ();
        }
    }
}
