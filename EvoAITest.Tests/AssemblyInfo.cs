using Microsoft.VisualStudio.TestTools.UnitTesting;

// Configure MSTest to enable parallelization at the class level
// This allows multiple test classes to run in parallel, improving test execution speed
[assembly: Parallelize(Workers = 0, Scope = ExecutionScope.ClassLevel)]
