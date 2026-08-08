# Third-party content

**This repository redistributes no third-party content.** Every tracked file is authored here and
covered by [`LICENSE`](LICENSE) (MIT). There is no vendored source, no submodule, and no copied
specification, documentation, or sample from any upstream project.

## The example corpus is original work

`examples/language-features/` is the one part of this repository that could be mistaken for derived
content, so it is worth being explicit.

**What comes from Microsoft's documents:** the *checklist*. Which features exist, which language
version introduced each one, and what each is called — taken from `Language-Version-History.md` in
`dotnet/csharplang` and the VB.NET what's-new pages. `MANIFEST.md` cites the section each row came
from, so the sourcing is auditable rather than implied.

**What does not:** the code. Every example is written here to demonstrate the feature named in its
row. None is copied or adapted from an upstream sample, a specification listing, or a documentation
page. Several carry comments recording hazards found by *running* the code — findings that exist
nowhere upstream because they were produced here.

A feature's name and the version that shipped it are facts. The examples that demonstrate them are
this project's expression, and are MIT-licensed along with everything else in the tree.

## If third-party content is ever added

1. Read the upstream `LICENSE` **at the commit being taken from**, and confirm the license actually
   covers the specific files. A repository-wide license does not always cover every subdirectory.
2. Record it in this file: what was taken, from which repository and commit, under which license,
   and where it now lives in this tree.
3. Reproduce the required attribution and license text wherever that license requires it.

This file records the project's policy and the factual state of the tree. It is not legal advice.
