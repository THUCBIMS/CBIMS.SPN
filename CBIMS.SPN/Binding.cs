// Copyright (C) 2023  Liu, Han; School of Software, Tsinghua University
//
// This file is part of CBIMS.SPN.
// CBIMS.SPN is free software: you can redistribute it and/or modify it under the terms of the GNU Lesser General Public License as published by the Free Software Foundation, either version 3 of the License, or (at your option) any later version.
// CBIMS.SPN is distributed in the hope that it will be useful, but WITHOUT ANY WARRANTY; without even the implied warranty of MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the GNU Lesser General Public License for more details.
// You should have received a copy of the GNU Lesser General Public License along with CBIMS.SPN. If not, see <https://www.gnu.org/licenses/>.

using CBIMS.LDP.Def;
using System;
using System.Collections.Generic;
using System.Text;
using VDS.RDF;
using VDS.RDF.Nodes;

namespace CBIMS.SPN
{
    public class Binding : Dictionary<string, INode>
    {
        public Binding() { }
        public Binding(Binding other) 
        { 
            foreach(var k in other.Keys)
            {
                this[k] = other[k];
            }
        }
        public bool HasArg(string arg) => this.ContainsKey(arg);
        public INode GetArg(string arg) => this[arg];

        private void AddArg(string key, INode val) { this[key] = val; }
        private void AddArg(string key, string val) { AddArg(key, val.ToStringNode(null)); }
        private void AddArg(string key, int val) { AddArg(key, val.ToLongNode(null)); }
        private void AddArg(string key, double val) { AddArg(key, val.ToDoubleNode(null)); }
        private void AddArg(string key, bool val) { AddArg(key, val.ToBooleanNode(null)); }

        private void AddArg(string key, IRdfTerm term) { AddArg(key, term.ToNode(null)); }

        public Binding AddArg(string key, object value)
        {
            if (value == null)
                throw new InvalidOperationException("null arg");
            else if (value is INode node)
                AddArg(key, node);
            else if (value is IRdfTerm term)
                AddArg(key, term);
            else if (value is string strVal)
                AddArg(key, strVal);
            else if (value is int intVal)
                AddArg(key, intVal);
            else if (value is double doubleVal)
                AddArg(key, doubleVal);
            else if (value is bool boolVal)
                AddArg(key, boolVal);
            else
                throw new InvalidCastException("AddArg() not implemented: " + value.GetType().FullName);
            return this;
        }

    }

    public class BindingOptionMap
    {
        public Transition Host { get; }
        private Dictionary<ArcP2T, List<Binding>> Options = new Dictionary<ArcP2T, List<Binding>>();

        public BindingOptionMap(Dictionary<ArcP2T, List<Binding>> options)
        {
            Options = options;
        }

        static Random RANDOM = new Random();
        private Dictionary<ArcP2T, int> dimensions = null;

        private int bindCount = -1;
        public int BindCount
        {
            get
            {
                if (bindCount == -1)
                {
                    dimensions = new Dictionary<ArcP2T, int>();
                    if (Options.Count > 0)
                    {
                        bindCount = 1;
                        foreach (var arc in Options.Keys)
                        {
                            var opts = Options[arc];
                            dimensions.Add(arc, opts.Count);
                            bindCount *= opts.Count;
                        }
                    }
                    else
                    {
                        bindCount = 0;
                    }
                }
                return bindCount;
            }
        }

        public Dictionary<string, INode> GetBinding(int i = -1)
        {
            if (i < 0)
            {
                i = RANDOM.Next(BindCount);
            }

            if (i >= BindCount)
            {
                return null;
            }

            //Console.WriteLine(i+"/"+ BindCount);

            Dictionary<string, INode> output = new Dictionary<string, INode>();

            int ii = i;

            foreach (var arc in Options.Keys)
            {
                var recs = Options[arc];
                var d = dimensions[arc];

                int j = ii % d;

                ii = ii / d;

                Binding rec = recs[j];
                foreach (var arg in rec.Keys)
                {
                    if (!output.ContainsKey(arg))
                    {
                        output[arg] = rec[arg];
                    }
                    else
                    {
                        throw new NotImplementedException("arg conflict.");
                    }
                }
            }

            return output;


        }


    }
}
