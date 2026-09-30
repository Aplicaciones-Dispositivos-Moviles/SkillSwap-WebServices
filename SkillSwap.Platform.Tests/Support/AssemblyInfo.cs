// All integration and BDD tests share one database, so they must not run in parallel.
[assembly: CollectionBehavior(DisableTestParallelization = true)]