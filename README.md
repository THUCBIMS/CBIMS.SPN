# CBIMS.SPN


## Introduction

CBIMS.SPN is an implentation of the Semantic Petri-Net (SPN). 

SPN is based on the Coloured Petri-Net (CPN) method, with extenions to access the RDF resources. A process model can be created with `transitions`, `places` and `arcs` in an RDF graph, and the state change rules in the process model are written in SPARQL.

Please refer to:

*Liu H, Song X Y, Gao G, Zhang H H, Liu Y S, Gu M.* 
**Modeling and Validating Temporal Rules with Semantic Petri-Net for Digital Twins.**
Advanced Engineering Informatics 57 (2023): 102099.
DOI: https://doi.org/10.1016/j.aei.2023.102099

*Liu H, Song X Y, Gao G, Zhang H H, Liu Y S, Gu M.* 
**Modeling and Validating Temporal Rules with Semantic Petri-Net for Digital Twins.**
International Workshop on Intelligent Computing in Engineering (EG-ICE), 2022:23-33. 
DOI: https://doi.org/10.7146/aul.455.c193


## Dependencies and resources

The project relies on `CBIMS.LDP` libraries (https://github.com/highan911/CBIMS.LDP).  
On compiling this project, please put the folder adjacent to `CBIMS.LDP`.   
Please refer to the README file in `CBIMS.LDP` for the details about its dependencies.


Other resources contained in the repository:

* `example_data/Office-compressed.7z/Office-compressed.ifc` - An example IFC file from the US National Institute of Building Sciences (NIBS) "Common Building Information Model Files and Tools" (https://www.wbdg.org/bim/cobie/common-bim-files). The model is converted from 3 IFC2X3 files and merged into one IFC4file.

## Folders in the repository

`CBIMS.SPN` is the implementation of the Semantic Petri-Net.

`Run` contains the scripts for running the SPN test cases. 

## Usage

[TODO]

## License

CBIMS.SPN libraries use `GNU Lesser General Public License (LGPL)`. 
Please refer to:
https://www.gnu.org/licenses/lgpl-3.0.en.html

## About CBIMS

**CBIMS** (Computable Building Information Modeling Standards) is a set of research projects aiming at the development of computable BIM standards for the interoperability of BIM data exchange and the automation of BIM-based workflow. The projects are led by the BIM research group at the School of Software, Tsinghua University, China.

