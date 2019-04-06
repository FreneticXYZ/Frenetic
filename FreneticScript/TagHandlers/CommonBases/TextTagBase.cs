//
// This file is part of FreneticScript, created by Frenetic LLC.
// This code is Copyright (C) Frenetic LLC under the terms of the MIT license.
// See README.md or LICENSE.txt in the FreneticScript source root for the contents of the license.
//

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using FreneticScript.TagHandlers.Objects;

namespace FreneticScript.TagHandlers.CommonBases
{
    // <--[explanation]
    // @Name Text Tags
    // @Description
    // TextTags are any random text, built into the tag system.
    // TODO: Explain better
    // TODO: Link tag system explanation
    // -->

    /// <summary>
    /// Handles the 'text' tag base.
    /// </summary>
    public class TextTagBase : TemplateTagBase
    {
        // <--[tagbase]
        // @Base text[<TextTag>]
        // @Group Common Base Types
        // @ReturnType TextTag
        // @Returns the input text as a TextTag.
        // <@link explanation Text Tags>What are text tags?<@/link>
        // -->

        /// <summary>
        /// Constructs the tag base data.
        /// </summary>
        public TextTagBase()
        {
            Name = "text";
            ResultTypeString = TextTag.TYPE;
        }

        /// <summary>
        /// Handles the base input for a tag.
        /// </summary>
        /// <param name="data">The tag data.</param>
        /// <returns>The correct object.</returns>
        public static TextTag HandleOne(TagData data)
        {
            return new TextTag(data.GetModifierCurrent());
        }
    }
}
