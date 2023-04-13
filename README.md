# CBIMS.SPN


## Introduction

CBIMS.SPN is an implentation of the Semantic Petri-Net (SPN). 

SPN is based on the Coloured Petri-Net (CPN) method, with extenions to access the RDF resources. A process model can be created with `transitions`, `places` and `arcs` in an RDF graph, and the state change rules in the process model are written in SPARQL.

Please refer to:

*Liu H, Song X Y, Gao G, Zhang H H, Liu Y S, Gu M.* 
**Modeling and Validating Temporal Rules with Semantic Petri-Net for Digital Twins.**
International Workshop on Intelligent Computing in Engineering (EG-ICE), 2022:23-33. 
DOI: https://doi.org/10.7146/aul.455.c193


## Dependencies and resources

The project uses the following dependencies from the NuGet:

* `dotNetRDF` (https://github.com/dotnetrdf)
* `Xbim` (https://github.com/xBimTeam/XbimEssentials, https://github.com/xBimTeam/XbimGeometry)
* `MathNet.Numerics` (https://github.com/mathnet/mathnet-numerics)
* `Newtonsoft.Json` (https://github.com/JamesNK/Newtonsoft.Json)
* `RTree` (https://github.com/drorgl/cspatialindexrt)

Other resources contained in the repository:

* `example_data/Office-compressed.7z/Office-compressed.ifc` - An example IFC file from the NIBS "Common Building Information Model Files and Tools" (https://www.wbdg.org/bim/cobie/common-bim-files). The model is conterted from IFC2X3 to IFC4 and merged into a single file.
* `CBIMS.LDP.IFC/Resource/ifcowl_ifc4.ttl` - The ifcOWL definitions for IFC4 (https://standards.buildingsmart.org/IFC/DEV/IFC4/ADD2_TC1/OWL).


## Folders in the repository

`CBIMS.SPN` is the implementation of the Semantic Petri-Net.

`Run` contains the scripts for running the SPN test cases. 

Dependencies included in the repository:

* `CBIMS.LDP.Def` - Commonly used RDFS/OWL definitions, and classes for interact with the dotNetRDF toolkit.
* `CBIMS.LDP.Repo` - Classes for implementing the Linked Data Platform (LDP) containers.
* `CBIMS.LDP.IFC` - Abstract classes for manipulating the Industry Foundation Classes (IFC) data.
* `CBIMS.LDP.IFC.XbimLoader` - Loading IFC data using the Xbim toolkit.

Dependencies loaded as binary library:

* `lib/CBIMS.CommonGeom` - Library for simple geometry calculations.


## Usage

[TODO]

## License

[TODO]

## About CBIMS

**CBIMS** (Computable Building Information Modeling Standards) is a set of research projects aiming at the development of computable BIM standards for the interoperability of BIM data exchange and the automation of BIM-based workflow. The projects are led by the BIM research group at the School of Software, Tsinghua University, China.

