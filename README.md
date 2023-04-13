# CBIMS.SPN


## Introduction

CBIMS.SPN is an implentation of the Semantic Petri-Net (SPN). 

SPN is based on the Coloured Petri-Net (CPN) method, with extenions to access the RDF resources. A process model can be created with `transitions`, `places` and `arcs` in an RDF graph, and the state change rules are written in SPARQL.


Please refer to:

*Liu H, Song X Y, Gao G, Zhang H H, Liu Y S, Gu M.* 
**Modeling and Validating Temporal Rules with Semantic Petri-Net for Digital Twins.**
International Workshop on Intelligent Computing in Engineering (EG-ICE), 2022:23-33. 
DOI: https://doi.org/10.7146/aul.455.c193


## Dependencies

The project uses the following tookits from the NuGet:

* `dotNetRDF` (https://github.com/dotnetrdf)
* `Xbim` (https://github.com/xBimTeam/XbimEssentials, https://github.com/xBimTeam/XbimGeometry)
* `MathNet.Numerics` (https://github.com/mathnet/mathnet-numerics)
* `Newtonsoft.Json` (https://github.com/JamesNK/Newtonsoft.Json)
* `RTree` (https://github.com/drorgl/cspatialindexrt)


## Folders in the repository

`CBIMS.SPN` is the implementation of the Semantic Petri-Net.

`Run` contains the scripts for running the SPN test cases. 

Dependencies included in the repository:

* `CBIMS.LDP.Def` - Commonly used RDFS/OWL definitions, and classes for interact with the dotNetRDF toolkit.
* `CBIMS.LDP.Repo` - Classes for implementing the Linked Data Platform (LDP) containers.
* `CBIMS.LDP.IFC` - Abstract classes for manipulating the Industry Foundation Classes (IFC) data.
* `CBIMS.LDP.IFC.XbimLoader` - Loading IFC data using the Xbim toolkit.

Dependencies loaded as binary library:

* `CBIMS.CommonGeom` - Library for simple geometry calculations.


## Usage

[TODO]

## License

[TODO]

## About CBIMS

CBIMS (Computable Building Information Modeling Standards) is a set of research projects aiming at the development of computable BIM standards for the interoperability of BIM data exchange and the automation of BIM-based workflow. The projects are led by the BIM research group at the School of Software, Tsinghua University, China.

