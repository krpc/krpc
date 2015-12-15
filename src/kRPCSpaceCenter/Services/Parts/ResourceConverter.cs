using System;
using KRPC.Service.Attributes;
using KRPC.Utils;
using KRPCSpaceCenter.ExtensionMethods;
using System.Collections.Generic;

namespace KRPCSpaceCenter.Services.Parts
{
    /// <summary>
    /// Obtained by calling <see cref="Part.ResourceConverter"/>
    /// </summary>
    [KRPCClass (Service = "SpaceCenter")]
    public sealed class ResourceConverter: Equatable<ResourceConverter>
    {
        readonly Part part;
        readonly List<ModuleResourceConverter> converters =new List<ModuleResourceConverter>() ;

        internal ResourceConverter (Part part)
        {
            this.part = part;
            foreach (PartModule m in part.InternalPart.Modules) {
                if (m is ModuleResourceConverter) {
                    var c = m as ModuleResourceConverter;
                    converters.Add (c);
                }
            }
   
        }

        public override bool Equals (ResourceConverter obj)
        {
            return part == obj.part;
        }

        public override int GetHashCode ()
        {
            return part.GetHashCode ();
        }

        /// <summary>
        /// The part object for this harvester;
        /// </summary>
        [KRPCProperty]
        public Part Part {
            get { return part; }
        }


        /// <summary>
        /// Returns the number of converter modules in the part
        /// </summary>
        [KRPCProperty]
        public int Count {
            get { return converters.Count;}
        }

        /// <summary>
        /// Returns True if Converter is activated
        /// </summary>
        [KRPCMethod]
        public bool Active(int c) { return converters [c].IsActivated; }
       


        /// <summary>
        /// Grabs the name of the specified converter
        /// </summary>
        [KRPCMethod]
        public string Name(int c) { return converters [c].ConverterName; }

        /// <summary>
        /// Starts the specified converter
        /// </summary>
        [KRPCMethod]
        public void Start (int c)
        { converters [c].StartResourceConverter (); }

        /// <summary>
        /// Stops the specified converter
        /// </summary>
        [KRPCMethod]
        public void Stop (int c)
        { converters [c].StopResourceConverter (); }

        /// <summary>
        /// Gets status of specified converter
        /// </summary>
        [KRPCMethod]
        public string Status (int c)
        {return converters [c].status; }

        /// <summary>
        /// Gets csv list of input resources
        /// </summary>
        [KRPCMethod]
        public string Inputs (int c)
        {
            string holder = "";
            foreach (ResourceRatio r in converters [c].inputList) {
                if (holder != "") {holder += ",";}
                holder += r.ResourceName;
            }

            return holder;
        }

        /// <summary>
        /// Gets csv list of output resources
        /// </summary>
        [KRPCMethod]
        public string Outputs (int c)
        {
            string holder = "";
            foreach (ResourceRatio r in converters [c].outputList) {
                if (holder != "") {holder += ",";}
                holder += r.ResourceName;
            }

            return holder;
        }


    }
}

