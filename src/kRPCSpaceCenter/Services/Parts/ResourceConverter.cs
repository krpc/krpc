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
        /// The part object for this harvester
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
        public bool Active(int c) {
            if(c<=converters.Count) return converters [c].IsActivated;
            else throw new ArgumentException ("Requested resource_converter does not exist in this part");
        }
       


        /// <summary>
        /// Returns the name of the specified converter
        /// </summary>
        [KRPCMethod]
        public string Name(int c) {
            if(c<=converters.Count) return converters [c].ConverterName; 
            else throw new ArgumentException ("Requested resource_converter does not exist in this part");
        }

        /// <summary>
        /// Starts the specified converter
        /// </summary>
        [KRPCMethod]
        public void Start (int c)
        { 
            if(c<=converters.Count) converters [c].StartResourceConverter (); 
            else throw new ArgumentException ("Requested resource_converter does not exist in this part");
        }

        /// <summary>
        /// Stops the specified converter
        /// </summary>
        [KRPCMethod]
        public void Stop (int c)
        {
            if(c<=converters.Count) converters [c].StopResourceConverter (); 
            else throw new ArgumentException ("Requested resource_converter does not exist in this part");
        }

        /// <summary>
        /// Gets status of specified converter
        /// </summary>
        [KRPCMethod]
        public string Status (int c)
        {
            if(c<=converters.Count) return converters [c].status; 
            else throw new ArgumentException ("Requested resource_converter does not exist in this part");
        }

        /// <summary>
        /// Gets list<string> of input resources
        /// </summary>
        [KRPCMethod]
        public IList<string> Inputs (int c)
        {
            if (c <= converters.Count) {
                List<string> holder=new List<string>();
                foreach (ResourceRatio r in converters [c].inputList) {
                    holder.Add (r.ResourceName);
                }

                return holder;
            }
            else throw new ArgumentException ("Requested resource_converter does not exist in this part");
        }

        /// <summary>
        /// Gets list<string>  of output resources
        /// </summary>
        [KRPCMethod]
        public IList<string> Outputs (int c)
        {
            if (c <= converters.Count) {
                List<string> holder=new List<string>();
                foreach (ResourceRatio r in converters [c].outputList) {
                    holder.Add(r.ResourceName);
                }

                return holder;
            }
            else throw new ArgumentException ("Requested resource_converter does not exist in this part");

        }
    }
}

