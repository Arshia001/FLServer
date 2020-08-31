using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace FLGrainInterfaces.Configuration
{
    public class TutorialGameSubjectConfig
    {
        public TutorialGameSubjectConfig(uint groupID, IReadOnlyList<string> categories)
        {
            GroupID = groupID;
            Categories = categories;
        }

        public uint GroupID { get; }
        public IReadOnlyList<string> Categories { get; } = Array.Empty<string>();

        public void Validate(int index, IReadOnlyList<GroupConfig> groups, IReadOnlyList<CategoryConfig> categories)
        {
            if (!groups.Any(g => g.ID == GroupID))
                Validation.FailWith($"Unknown group ID {GroupID} in tutorial game subject config at index {index}");

            foreach (var categoryName in Categories)
            {
                var category = categories.FirstOrDefault(c => c.Name == categoryName);
                if (category == null || category.Group.ID != GroupID)
                    Validation.FailWith($"Invalid category {categoryName} in group {GroupID} in tutorial game subject config at index {index}");
            }
        }
    }
}
