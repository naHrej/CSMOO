This document +dictates the design guildelines that AI code generators must follow for this project.

Prefer at all times to pass object references to and return object references from methods.
Except for a very few functions, we do NOT want to continually convert objects to IDs, pass their IDs to other functions, and then look up the object itself again in another function.  We want to reduce this inefficiency as much as possible.

Prefer at all times to use the abbreviation "ID" in all caps for identifiers.  
For example, "PlayerID" versus "PlayerId".  "Id" is a word, while "ID" is an abbreviation.

Reduce code duplication by reusing functions.  We have six different copies of ObjectResolver at last check and this is unacceptable.  Especially given the fact that they frequently have differing method signatures yet the same functionality.  If we wanted crappy code, we'd have written it ourselves.

Prefer at all times to use file-scoped namespaces over block-scoped namespaces.

