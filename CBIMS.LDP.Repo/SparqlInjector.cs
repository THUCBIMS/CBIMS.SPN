using CBIMS.LDP.Def;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using VDS.RDF;
using VDS.RDF.Parsing;
using VDS.RDF.Query;
using VDS.RDF.Query.Patterns;
using VDS.RDF.Writing.Formatting;

namespace CBIMS.LDP.Repo
{
    

    public class SparqlInjectee
    {        
        // Injecting Prefixs into Header
        public Dictionary<string, Uri> PrefixNC = new Dictionary<string, Uri>();

        public List<string> FromGraph = new List<string>(); //qname or <Uri>
        public List<string> FromNamedGraph = new List<string>(); //qname or <Uri>

        // Injecting Values and Binds into Body

        public Dictionary<string, INode> Binds = new Dictionary<string, INode>();

        public ArgTuples Values = new ArgTuples();

        public SparqlInjectee Clone()
        {
            var output = new SparqlInjectee();
            output.Values = this.Values.Clone();
            output.Binds = new Dictionary<string, INode>(this.Binds);
            return output;
        }

        public void ClearHeader()
        {
            PrefixNC.Clear();
            FromGraph.Clear();
            FromNamedGraph.Clear();
        }

        public void ClearBody()
        {
            Binds.Clear();
            Values = new ArgTuples();
        }

        public void UnionPrefix(Dictionary<string, Uri> prefix)
        {
            foreach(var p in prefix.Keys)
            {
                PrefixNC[p] = prefix[p];
            }
        }


    }
    internal class SparqlInjector
    {


        public SparqlInjectee Injectee { get; set; }

        public string OrininalSPARQL { get; }

        public TurtleFormatter Formatter { get; }

        private string _Header { get; }
        private string _Body { get; }


        public SparqlInjector(string SPARQL, TurtleFormatter formatter)
        {
            Formatter = formatter;
            OrininalSPARQL = SPARQL.Trim();

            int first_brace = OrininalSPARQL.IndexOf("{");

            _Header = OrininalSPARQL.Substring(0, first_brace).TrimEnd();

            if (_Header.EndsWith("WHERE"))
            {
                _Header = _Header.Substring(0, _Header.Length - 5).TrimEnd();
            }

            _Body = OrininalSPARQL.Substring(first_brace + 1);

        }

        public string ToSPARQL()
        {

            if (Injectee == null)
            {
                return $"{_Header}\nWHERE {{\n{_Body}";
            }
            StringBuilder sb = new StringBuilder();

            // Header

            var prefix = Injectee.PrefixNC;

            foreach (var k in prefix.Keys)
            {
                sb.AppendLine($"PREFIX {k}: <{prefix[k].AbsoluteUri}>");
            }

            sb.AppendLine(_Header);

            foreach (var graph in Injectee.FromGraph)
            {
                sb.AppendLine($"FROM {graph}");
            }
            foreach (var named in Injectee.FromNamedGraph)
            {
                sb.AppendLine($"FROM NAMED {named}");
            }

            sb.AppendLine("WHERE {");

            // Body

            var binds = Injectee.Binds;

            foreach (var bindKey in binds.Keys)
            {
                var nodeStr = Formatter.Format(binds[bindKey]);
                sb.AppendLine($"BIND ({nodeStr} AS {bindKey})");
            }

            var values = Injectee.Values;
            if (values.Args.Length > 0 && values.Recs.Count > 0)
            {
                int argCount = values.Args.Length;

                sb.AppendLine($"VALUES ({string.Join(" ", values.Args)}){{");

                string _NULL_ = "\"_NULL_\"";

                foreach (INode[] item in values.Recs)
                {
                    if (item.Length != argCount)
                    {
                        throw new InvalidOperationException("values count mismatch");
                    }

                    string[] strs = item.Select(t => {
                        if (t != null)
                            return Formatter.Format(t);
                        else
                            return _NULL_;
                    }).ToArray();
                    sb.AppendLine($"({string.Join(" ", strs)})");
                }
                sb.AppendLine("}");
            }

            sb.AppendLine(_Body);

            return sb.ToString();
        }
        public override string ToString()
        {
            return ToSPARQL();
        }
    }
}
