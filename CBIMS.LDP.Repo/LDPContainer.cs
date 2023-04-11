using CBIMS.LDP.Def;
using System;
using System.Collections.Generic;
using System.Linq;
using VDS.RDF;

namespace CBIMS.LDP.Repo
{
    public enum MemberResourceLocation
    {
        NONE, SUBJ, OBJ
    }


    public class LDPContainer : RdfInstPersist
    {
        public const string PROP_LDP_CONTAINS = "ldp:contains";

        // events
        public delegate void LDPContainerEventHandler(LDPContainer sender, object v);

        public event LDPContainerEventHandler ContentAdded;
        public event LDPContainerEventHandler ContentRemoved;

        public LDPContainer(RdfNSDef ns, string name, IRdfClassDef type = null, IUriNode node = null) : base(ns, name, type, node)
        {
            if (type == null)
            {
                AddType(RepoDefs.BasicContainer);
            }

            if(type == RepoDefs.DirectContainer)
            {
                ContentAdded += ContentAdded_Direct;
                ContentRemoved += ContentRemoved_Direct;
            }

            if (type == RepoDefs.IndirectContainer)
            {
                ContentAdded += ContentAdded_Indirect;
                ContentRemoved += ContentRemoved_Indirect;
            }


        }

        // Basic

        public IEnumerable<object> Contents => GetProp(PROP_LDP_CONTAINS);
        public void AddContent(object v)
        {
            AddProp(PROP_LDP_CONTAINS, v);
            ContentAdded?.Invoke(this, v);
        }
        public void RemoveContent(object v)
        {
            if (Contains(v))
            {
                RemoveProp(PROP_LDP_CONTAINS, v);
                ContentRemoved?.Invoke(this, v);
            }
        }
        public bool Contains(object v) => HasPropVal(PROP_LDP_CONTAINS, v);

        public void Clear()
        {
            foreach(var item in Contents.ToArray())
            {
                RemoveContent(item);
            }
        }

        // Direct

        public MemberResourceLocation MemberResourceLocation
        {
            get
            {
                if(Graph != null && MembershipResourceNode != null)
                {
                    if (HasMemberRelation != null && IsMemberOfRelation == null)
                    {
                        return MemberResourceLocation.SUBJ;
                    }
                    else if(HasMemberRelation == null && IsMemberOfRelation != null)
                    {
                        return MemberResourceLocation.OBJ;
                    }
                }
                return MemberResourceLocation.NONE;
            }
        }
        public object MembershipResource {
            get => GetPropSingle("ldp:membershipResource");
            set => SetProp("ldp:membershipResource", value); 
        }
        public IUriNode MembershipResourceNode => MembershipResource.ToNode(Graph) as IUriNode;

        public IUriNode HasMemberRelation
        {
            get => GetPropSingle<IUriNode>("ldp:hasMemberRelation");
            set => SetProp("ldp:hasMemberRelation", value);
        }
        public IUriNode IsMemberOfRelation
        {
            get => GetPropSingle<IUriNode>("ldp:isMemberOfRelation");
            set => SetProp("ldp:isMemberOfRelation", value);
        }


        // Indirect

        public IUriNode InsertedContentRelation
        {
            get => GetPropSingle<IUriNode>("ldp:insertedContentRelation");
            set => SetProp("ldp:insertedContentRelation", value);
        }

        // Event handlers

        public static void ContentAdded_Direct(LDPContainer sender, object v)
        {
            var loc = sender.MemberResourceLocation;
            if (loc == MemberResourceLocation.SUBJ)
            {
                var node = v.ToNode(sender.Graph);
                if(node != null)
                {
                    sender.Graph.Assert(sender.MembershipResourceNode, sender.HasMemberRelation, node);
                }
            }
            else if (loc == MemberResourceLocation.OBJ)
            {
                var node = v.ToNode(sender.Graph);
                if (node != null && node is IUriNode)
                {
                    sender.Graph.Assert(node, sender.IsMemberOfRelation, sender.MembershipResourceNode);
                }
            }
        }
        public static void ContentRemoved_Direct(LDPContainer sender, object v)
        {
            var loc = sender.MemberResourceLocation;
            if (loc == MemberResourceLocation.SUBJ)
            {
                var node = v.ToNode(sender.Graph);
                if (node != null)
                {
                    sender.Graph.Retract(sender.MembershipResourceNode, sender.HasMemberRelation, node);
                }
            }
            else if (loc == MemberResourceLocation.OBJ)
            {
                var node = v.ToNode(sender.Graph);
                if (node != null && node is IUriNode)
                {
                    sender.Graph.Retract(node, sender.IsMemberOfRelation, sender.MembershipResourceNode);
                }
            }
        }
        public static void ContentAdded_Indirect(LDPContainer sender, object v)
        {
            if (sender.InsertedContentRelation == null)
            {
                ContentAdded_Direct(sender, v);
                return;
            }

            var loc = sender.MemberResourceLocation;
            if (loc == MemberResourceLocation.NONE)
                return;

            IEnumerable<INode> targets = _FindTargets(sender, v);

            if (targets == null || targets.Count() == 0)
                return;

            if (loc == MemberResourceLocation.SUBJ)
            {
                foreach (var target in targets)
                {
                    sender.Graph.Assert(sender.MembershipResourceNode, sender.HasMemberRelation, target);
                }
            }
            else if (loc == MemberResourceLocation.OBJ)
            {
                foreach (var target in targets)
                {
                    sender.Graph.Assert(target, sender.IsMemberOfRelation, sender.MembershipResourceNode);
                }
            }
        }

        public static void ContentRemoved_Indirect(LDPContainer sender, object v)
        {
            // TODO: what if multiple content has the same indirect content?

            if (sender.InsertedContentRelation == null)
            {
                ContentRemoved_Direct(sender, v);
                return;
            }

            var loc = sender.MemberResourceLocation;
            if (loc == MemberResourceLocation.NONE)
                return;

            IEnumerable<INode> targets = _FindTargets(sender, v);

            if (targets == null || targets.Count() == 0)
                return;

            if (loc == MemberResourceLocation.SUBJ)
            {
                foreach (var target in targets)
                {
                    sender.Graph.Retract(sender.MembershipResourceNode, sender.HasMemberRelation, target);
                }
            }
            else if (loc == MemberResourceLocation.OBJ)
            {
                foreach (var target in targets)
                {
                    sender.Graph.Retract(target, sender.IsMemberOfRelation, sender.MembershipResourceNode);
                }
            }

        }


        private static IEnumerable<INode> _FindTargets(LDPContainer sender, object v)
        {
            // TODO: FindTargets from multiple graph, through queriable repository

            IEnumerable<INode> targets = null;
            if (v is IRdfInst inst)
            {
                var trips = inst.Graph.GetTriplesWithSubjectPredicate(inst.Node, sender.InsertedContentRelation);
                targets = trips.Select(t => t.Object);
            }
            else if (v is IRdfModel model)
            {
                var trips = model.Graph.GetTriplesWithSubjectPredicate(model.Node, sender.InsertedContentRelation);
                targets = trips.Select(t => t.Object);
            }
            else if (v is IUriNode UNode)
            {
                var trips = UNode.Graph.GetTriplesWithSubjectPredicate(UNode, sender.InsertedContentRelation);
                targets = trips.Select(t => t.Object);
            }
            else
            {
                throw new NotImplementedException();
            }
            return targets;
        }


    }

    public class LDPContainer<T> : LDPContainer
    {
        public LDPContainer(RdfNSDef ns, string name, IRdfClassDef type = null, IUriNode node = null) : base(ns, name, type, node)
        {
        }
        public new IEnumerable<T> Contents => base.Contents?.Cast<T>();
        public void AddContent(T v) => base.AddContent(v);
        public void RemoveContent(T v) => base.RemoveContent(v);
        public bool Contains(T v) => base.Contains(v);
    }

}
