﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Principal;
using System.Text;
using System.Threading.Tasks;
using Scoring_Report.Configuration;
using Scoring_Report.Configuration.UserRights;
using Scoring_Report.Policies;

namespace Scoring_Report.Scoring.Sections
{
    public class SectionUserRights : ISection
    {
        public string Header => "User Rights Assignment:";

        public static List<UserRightsDefinition> ConfigPolicy => ConfigurationManager.UserRightsDefinitions;

        public static UserRightsAssignment SystemPolicy => SecurityPolicyManager.Settings.LocalPolicies.UserRightsAssignment;

        public const string Format = "'{0}' set correctly - {1}";

        public int MaxScore()
        {
            // Return number of scored user rights definitions
            return ConfigurationManager.UserRightsDefinitions.Count;
        }

        public string GetNameFromSID(SecurityIdentifier identifier)
        {
            // Translates SID to name from local groups/users or accessible domain info
            string name = identifier.Translate(typeof(NTAccount)).ToString();

            // Gets last section of name, for example, without
            // this something may be like 'BUILTIN\\Backup Operators'
            name = name.Split('\\').Last();

            return name;
        }

        public SectionDetails GetScore()
        {
            SectionDetails details = new SectionDetails(0, new List<string>(), this);

            SecurityPolicyManager.GetUserRightsAssignment();

            // For each config definition
            foreach (UserRightsDefinition definition in ConfigPolicy)
            {
                // Create copy of dictionary. Uses more memory but optimizes
                // checking as we can remove user rights after checking them
                Dictionary<string, List<SecurityIdentifier>> tempDict = 
                    new Dictionary<string, List<SecurityIdentifier>>(SystemPolicy.UserRightsSetting);

                string foundUserRights = null;
                bool configCorrect = true;

                // For each retrieved system user rights definition
                foreach (KeyValuePair<string, List<SecurityIdentifier>> userRights in tempDict)
                {
                    // Check if config and system define the same user rights
                    if (definition.ConstantName != userRights.Key) continue;

                    foundUserRights = definition.ConstantName;

                    // If config and user rights identifiers count do not match,
                    // it is incorrectly configured. Break the loop and set as incorrect
                    if (definition.Identifiers.Count != userRights.Value.Count)
                    {
                        configCorrect = false;
                        break;
                    }

                    // Loop over sids first so we're not converting 
                    // identifiers to usernames multiple times for each
                    foreach (SecurityIdentifier identifier in userRights.Value)
                    {
                        // Cache name to be compared with later checked identifiers
                        string name = null;

                        bool foundIdentifier = false;

                        foreach (UserRightsIdentifier cfgId in definition.Identifiers)
                        {
                            switch (cfgId.Type)
                            {
                                case EUserRightsIdentifierType.Name:
                                    // If name has not been found yet, retrieve it
                                    if (name == null)
                                    {
                                        name = GetNameFromSID(identifier);
                                    }

                                    // If name and config name match, match is found
                                    if (name == cfgId.Identifier)
                                    {
                                        foundIdentifier = true;
                                        
                                        // Save name for possible output
                                        cfgId.Name = name;
                                    }
                                    break;
                                case EUserRightsIdentifierType.SecurityID:
                                    // If Security IDs match, match is found
                                    if (identifier.Value == cfgId.Identifier)
                                    {
                                        foundIdentifier = true;

                                        // If name has not been found yet, retrieve it
                                        if (name == null)
                                        {
                                            name = GetNameFromSID(identifier);
                                        }

                                        // Save name for possible output
                                        cfgId.Name = name;
                                    }

                                    break;
                            }

                            // If we found the match, break the loop
                            // as there is no need to keep searching
                            if (foundIdentifier) break;
                        }

                        // If no match was found, the config and
                        // system config do not match, break the loop
                        if (!foundIdentifier)
                        {
                            configCorrect = false;
                            break;
                        }
                    }

                    // If process got here,
                    // break the loop as a match was found
                    break;
                }

                // If user rights was found
                if (foundUserRights != null)
                {
                    // If configured correctly, increment points and give output
                    if (configCorrect)
                    {
                        // Create list for every name, used to give user the proper output
                        IEnumerable<string> names = definition.Identifiers.Select(x => x.Name);

                        details.Points++;
                        details.Output.Add(string.Format(Format, definition.Setting, string.Join(", ", names)));
                    }

                    tempDict.Remove(foundUserRights);
                }
            }

            return details;
        }
    }
}