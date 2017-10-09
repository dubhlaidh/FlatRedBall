﻿using GlueBuilder.Managers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GlueConsole
{
    class Program
    {
        static void Main(string[] args)
        {
            Console.WriteLine("Welcome to Glue Command Line");

            ContainerPopulator.PopulateDefaultContainers();

            while(true)
            {
                var line = Console.ReadLine();

                try
                {
                    CommandLineCommandProcessor.Process(line);
                }
                catch(Exception e)
                {
                    Console.WriteLine(e.ToString());
                }

                if(line == "exit")
                {
                    break;
                }
            }
        }
    }
}
