﻿using System.Collections;
using System.Text;

namespace Flee.Parsing
{
    internal class ParserLogException : Exception
    {
        private readonly ArrayList _errors = new();
        public ParserLogException()
        {
        }
        public override string Message
        {
            get
            {
                StringBuilder buffer = new();

                for (int i = 0; i < Count; i++)
                {
                    if (i > 0)
                    {
                        buffer.Append('\n');
                    }
                    buffer.Append(this[i].Message);
                }
                return buffer.ToString();
            }
        }

        public int Count => _errors.Count;


        public int GetErrorCount()
        {
            return Count;
        }

        public ParseException this[int index] => (ParseException)_errors[index];

        public ParseException GetError(int index)
        {
            return this[index];
        }

        public void AddError(ParseException e)
        {
            _errors.Add(e);
        }

        public string GetMessage()
        {
            return Message;
        }
    }
}
