﻿using System;
using System.Collections.Generic;
using System.Text;
using System.IO;

namespace DDay.iCal.Serialization.iCalendar
{
    public class PropertySerializer :
        SerializerBase
    {
        public override string SerializeToString(object obj)
        {
            ICalendarProperty prop = obj as ICalendarProperty;
            if (prop != null)
            {
                StringBuilder sb = new StringBuilder(prop.Name);
                if (prop.Parameters.Count > 0)
                {
                    ParameterSerializer parmSerializer = new ParameterSerializer();
                    parmSerializer.SerializationContext = SerializationContext;
                    
                    List<string> parameters = new List<string>();
                    foreach (ICalendarParameter param in prop.Parameters)
                    {
                        parameters.Add(parmSerializer.SerializeToString(param));
                    }

                    sb.Append(";");
                    sb.Append(string.Join(";", parameters.ToArray()));
                }
                sb.Append(":");
                sb.Append(prop.Value);

                // FIXME: use some extra method here to perform
                // line wrapping at 75 characters
                return sb.ToString();
            }
            return null;            
        }

        public override object Deserialize(TextReader tr)
        {
            // Create a lexer for our text stream
            iCalLexer lexer = new iCalLexer(tr);
            iCalParser parser = new iCalParser(lexer);

            // Get our serialization context
            ISerializationContext ctx = SerializationContext;

            // Get a serializer factory from our serialization services
            ISerializerFactory sf = GetService<ISerializerFactory>();

            // Parse the component!
            ICalendarProperty p = parser.property(ctx, sf, null);

            // Close our text stream
            tr.Close();

            // Return the parsed property
            return p;
        }
    }
}