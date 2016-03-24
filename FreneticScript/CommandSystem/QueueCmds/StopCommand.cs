﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using FreneticScript.TagHandlers;
using FreneticScript.TagHandlers.Objects;

namespace FreneticScript.CommandSystem.QueueCmds
{
    class StopCommand: AbstractCommand
    {
        public StopCommand()
        {
            Name = "stop";
            Arguments = "['all']";
            Description = "Stops the current command queue.";
            IsFlow = true;
            Asyncable = true;
            MinimumArguments = 0;
            MaximumArguments = 1;
            ObjectTypes = new List<Func<TemplateObject, TemplateObject>>()
            {
                (input) =>
                {
                    string inp = input.ToString().ToLowerFast();
                    if (inp == "all")
                    {
                        return new TextTag(inp);
                    }
                    return null;
                }
            };
        }

        public override void Execute(CommandEntry entry)
        {
            if (entry.Arguments.Count > 0 && entry.GetArgument(0).ToLowerFast() == "all")
            {
                int qCount = entry.Queue.CommandSystem.Queues.Count;
                if (!entry.Queue.CommandSystem.Queues.Contains(entry.Queue))
                {
                    qCount++;
                }
                if (entry.ShouldShowGood())
                {
                    entry.Good("Stopping <{text_color.emphasis}>" + qCount + "<{text_color.base}> queue" + (qCount == 1 ? "." : "s."));
                }
                foreach (CommandQueue queue in entry.Queue.CommandSystem.Queues)
                {
                    queue.Stop();
                }
                entry.Queue.Stop();
            }
            else
            {
                if (entry.ShouldShowGood())
                {
                    entry.Good("Stopping current queue.");
                }
                entry.Queue.Stop();
            }
        }
    }
}
