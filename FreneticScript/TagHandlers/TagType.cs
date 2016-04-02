﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace FreneticScript.TagHandlers
{
    /// <summary>
    /// Represents the specific type of a tag.
    /// </summary>
    public class TagType
    {
        /// <summary>
        /// The name of this tag type, lowercase.
        /// </summary>
        public string TypeName;

        /// <summary>
        /// The name of the type upon which this tag type is based.
        /// </summary>
        public string SubTypeName;

        /// <summary>
        /// The type upon which this tag type is based.
        /// </summary>
        public TagType SubType;

        /// <summary>
        /// This function should take the two inputs and return a valid object of the relevant type.
        /// </summary>
        public Func<TagData, TemplateObject, TemplateObject> TypeGetter;

        /// <summary>
        /// The tag sub-handler for all possible tags.
        /// </summary>
        public Dictionary<string, TagSubHandler> SubHandlers;

        /// <summary>
        /// Duplicates a tag type object.
        /// </summary>
        /// <returns>The new duplicate.</returns>
        public TagType Duplicate()
        {
            return (TagType)MemberwiseClone();
        }
    }
}
