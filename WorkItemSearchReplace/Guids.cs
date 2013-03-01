// Guids.cs
// MUST match guids.h
using System;

namespace MagenicTechnologies.WorkItemSearchReplace
{
    static class GuidList
    {
        public const string guidWorkItemSearchReplacePkgString = "c82069d2-5e08-4679-a566-0b61326d3465";
        public const string guidWorkItemSearchReplaceCmdSetString = "c084bc8f-5261-42d8-9699-24bc1093d8b6";

        public static readonly Guid guidWorkItemSearchReplaceCmdSet = new Guid(guidWorkItemSearchReplaceCmdSetString);
    };
}