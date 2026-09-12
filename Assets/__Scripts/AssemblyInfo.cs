using System.Runtime.CompilerServices;

[assembly: InternalsVisibleTo("Tests")]

// Allow edit-mode regression tests to verify legacy environment-removal migration without a Song Edit Menu scene.
[assembly: InternalsVisibleTo("TestsEditMode")]
