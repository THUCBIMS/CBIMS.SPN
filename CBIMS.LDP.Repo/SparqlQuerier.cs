using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using VDS.RDF;
using VDS.RDF.Nodes;
using VDS.RDF.Parsing;
using VDS.RDF.Query;
using VDS.RDF.Query.Expressions;
using VDS.RDF.Query.PropertyFunctions;

namespace CBIMS.LDP.Repo
{
    public class ArgTuples
    {
        public string[] Args = new string[] { };
        public List<INode[]> Recs = new List<INode[]>();
        public ArgTuples() { }
        public ArgTuples(string[] args)
        {
            if (args != null)
                Args = args;
        }

        public ArgTuples Clone()
        {
            ArgTuples output = new ArgTuples(this.Args);
            output.Recs = this.Recs.Select(r => r.ToArray()).ToList();
            return output;
        }
    }

    public class SparqlQuerier
    {

        private IQueryableRepository Repo;

        private SparqlQueryParser Parser { get; }
        private ISparqlQueryProcessor Processor { get; }


        private Dictionary<Type, ISparqlCustomExpressionFactory> FactoryMap { get; }
        private Dictionary<Type, IPropertyFunctionFactory> MagicPropertyFactoryMap { get; }

        public SparqlQuerier(IQueryableRepository repo)
        {
            Repo = repo;

            Parser = new SparqlQueryParser();
            Processor = new LeviathanQueryProcessor(repo.Dataset);
            FactoryMap = new Dictionary<Type, ISparqlCustomExpressionFactory>();
            MagicPropertyFactoryMap = new Dictionary<Type, IPropertyFunctionFactory>();
        }

        public void AddFactory(ISparqlCustomExpressionFactory factory)
        {
            if (_getFactoryByType(factory.GetType()) == null)
            {
                FactoryMap.Add(factory.GetType(), factory);

                List<ISparqlCustomExpressionFactory> factories = new List<ISparqlCustomExpressionFactory>(Parser.ExpressionFactories);
                factories.Add(factory);
                Parser.ExpressionFactories = factories;

            }
        }
        public void AddFactory(IPropertyFunctionFactory factory)
        {
            if (_getMagicPropertyFactoryByType(factory.GetType()) == null)
            {
                MagicPropertyFactoryMap.Add(factory.GetType(), factory);
            }
        }

        private ISparqlCustomExpressionFactory _getFactoryByType(Type type)
        {
            if (FactoryMap.ContainsKey(type))
            {
                return FactoryMap[type];
            }
            return null;
        }
        private IPropertyFunctionFactory _getMagicPropertyFactoryByType(Type type)
        {
            if (MagicPropertyFactoryMap.ContainsKey(type))
            {
                return MagicPropertyFactoryMap[type];
            }
            return null;
        }


        private SparqlResultSet _QueryForVal(string queryStr, SparqlInjectee injectee)
        {
            SparqlInjector injector = new SparqlInjector(queryStr, Repo.Formatter);
            injector.Injectee = injectee;
            if (injectee == null)
            {
                injector.Injectee = new SparqlInjectee();
            }
            injector.Injectee.UnionPrefix(GetSPARQLPrefixMap());
            queryStr = injector.ToSPARQL();


            SparqlQuery q = null;
            try
            {
                q = Parser.ParseFromString(queryStr);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"illegal query \t{queryStr}");
                Console.WriteLine($"{ex.GetType().Name}\t{ex.Message}");
                Console.WriteLine();
            }

            if (q != null)
                return _QueryForVal(q);
            throw new InvalidOperationException();
        }
        private IGraph _QueryForGraph(string queryStr, SparqlInjectee injectee, bool autoAddPrefix)
        {
            if (autoAddPrefix)
                queryStr = GetSPARQLPrefixStr() + queryStr;
            SparqlQuery q = null;
            try
            {
                q = Parser.ParseFromString(queryStr);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"illegal query \t{queryStr}");
                Console.WriteLine($"{ex.GetType().Name}\t{ex.Message}");
                Console.WriteLine();
            }
            if (q != null)
                return _QueryForGraph(q);
            throw new InvalidOperationException();
        }


        private SparqlResultSet _QueryForVal(SparqlQuery q)
        {
            q.PropertyFunctionFactories = MagicPropertyFactoryMap.Values;
            return Processor.ProcessQuery(q) as SparqlResultSet;
        }
        private IGraph _QueryForGraph(SparqlQuery q)
        {
            q.PropertyFunctionFactories = MagicPropertyFactoryMap.Values;
            return Processor.ProcessQuery(q) as IGraph;
        }


        public bool QueryForBoolean(string queryStr, SparqlInjectee injectee)
        {
            SparqlResultSet results = _QueryForVal(queryStr, injectee);
            
            if (results == null)
                throw new InvalidOperationException();
            
            if (results.ResultsType == SparqlResultsType.Boolean)
            {
                return results.Result;
            }
            else if(results.ResultsType == SparqlResultsType.VariableBindings)
            {
                return results.Count > 0;
            }
            else
            {
                throw new NotImplementedException();
            }
        }

        public ArgTuples QueryForTuple(string queryStr, SparqlInjectee injectee)
        {
            SparqlResultSet results = _QueryForVal(queryStr, injectee);

            if (results == null)
                throw new InvalidOperationException();
            
            if (results.ResultsType == SparqlResultsType.Boolean)
            {
                var output = new ArgTuples(new string[] { "?RESULT" });
                output.Recs.Add(new INode[] { new BooleanNode(null, results.Result) });
                return output;
            }
            else if (results.ResultsType == SparqlResultsType.VariableBindings)
            {
                var variables = results.Variables.ToArray();

                if(results.Results.Any())
                {
                    var first = results.Results.First();
                    if (first.Variables.Count() != variables.Length)
                    {
                        //some args do not have expression
                        variables = first.Variables.ToArray();
                    }
                }

                var output = new ArgTuples(variables.Select(t => $"?{t}").ToArray());

                //remove redundant values ... WHY they should be here?

                Dictionary<string, INode[]> resultsMap = new Dictionary<string, INode[]>();

                foreach(var result in results.Results)
                {
                    List<INode> _res = new List<INode>();
                    StringBuilder sb = new StringBuilder();
                    foreach (var key in variables)
                    {
                        var val = result.Value(key);
                        _res.Add(val);
                        sb.Append(val.ToString());
                        sb.Append("|");
                    }
                    var identifier = sb.ToString();
                    resultsMap[identifier] = _res.ToArray();
                }

                output.Recs.AddRange(resultsMap.Values);

                return output;
            }
            else
            {
                throw new NotImplementedException();
            }
        }


        public Dictionary<string, Uri> GetSPARQLPrefixMap()
        {
            Dictionary<string, Uri> output = new Dictionary<string, Uri>();
            foreach (var prefix in Repo.NamespaceMap.Prefixes)
            {
                output[prefix] = Repo.NamespaceMap.GetNamespaceUri(prefix);
            }
            return output;
        }

        public string GetSPARQLPrefixStr()
        {
            StringBuilder sb = new StringBuilder();
            foreach (var prefix in Repo.NamespaceMap.Prefixes)
            {
                sb.AppendLine($"PREFIX {prefix}: <{Repo.NamespaceMap.GetNamespaceUri(prefix)}>");
            }
            return sb.ToString();
        }

        //List<IUriNode> QueryInstancesByType(string graphUri, string qName)
        //{
        //    string queryStr = $"SELECT ?x FROM <{graphUri}> {{?x a/rdfs:subClassOf* {qName} }}";
        //    var q = Parser.ParseFromString(GetSPARQLPrefix() + queryStr);
        //    var results = ProcessQuery(q) as SparqlResultSet;

        //    List<IUriNode> res = new List<IUriNode>();
        //    var key = results.Variables.First();
        //    foreach (var result in results.Results)
        //    {
        //        var value = result.Value(key) as IUriNode;
        //        if (value != null)
        //        {
        //            res.Add(value);
        //        }
        //    }
        //    return res;
        //}


    }
}
