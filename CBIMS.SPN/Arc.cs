using CBIMS.LDP.Def;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using VDS.RDF;

namespace CBIMS.SPN
{

    public class AbstractArc : RdfInstPersist
    {
        public SPNModel Model { get; }
        public Place relPlace => GetPropSingle<Place>("spn:relPlace");
        public Transition relTransition => GetPropSingle<Transition>("spn:relTransition");
        public AbstractRule arcExpr => GetPropSingle<AbstractRule>("spn:arcExpr"); // from binding to token
        public IEnumerable<ArgDef> hasArg => GetProp<ArgDef>("spn:hasArg");

        protected AbstractArc(SPNModel model, string name, Place relPlace, Transition relTransition, AbstractRule arcExpr, IEnumerable<ArgDef> args, IRdfClassDef type, IUriNode node = null) : base(model.NS, name, type, node)
        {
            Model = model;
            if (args == null || !args.Any())
            {
                throw new InvalidOperationException($"args can not be null: arc {name}");
            }
            
            if(arcExpr == null && args.Count() > 1)
            {
                throw new InvalidOperationException("only one arg supported when arcExpr is not set");
            }

            SetProp("spn:relPlace", relPlace);
            SetProp("spn:relTransition", relTransition);
            SetProp("spn:arcExpr", arcExpr);
            SetProps("spn:hasArg", args);
        }
    }
    public class ArcP2T : AbstractArc
    {
        public AbstractRule arcExprInv => GetPropSingle<AbstractRule>("spn:arcExprInv"); // from token to available binding

        internal ArcP2T(SPNModel model, string name, Place relPlace, Transition relTransition, 
            AbstractRule arcExpr, AbstractRule arcExprInv, IEnumerable<ArgDef> args, IUriNode node = null) : base(model, name, relPlace, relTransition, arcExpr, args, SPNDefs.ArcP2T, node)
        {
            SetProp("spn:arcExprInv", arcExprInv);
        }


    }

    public class ArcT2P : AbstractArc
    {
        internal ArcT2P(SPNModel model, string name, Place relPlace, Transition relTransition, AbstractRule arcExpr, IEnumerable<ArgDef> args, IUriNode node = null) : base(model, name, relPlace, relTransition, arcExpr, args, SPNDefs.ArcT2P, node)
        {
        }
    }
}
