//
// Copyright (c) 2007 Ole André Vadla Ravnås <oleavr@gmail.com>
//
// Permission is hereby granted, free of charge, to any person
// obtaining a copy of this software and associated documentation
// files (the "Software"), to deal in the Software without
// restriction, including without limitation the rights to use,
// copy, modify, merge, publish, distribute, sublicense, and/or sell
// copies of the Software, and to permit persons to whom the
// Software is furnished to do so, subject to the following
// conditions:
//
// The above copyright notice and this permission notice shall be
// included in all copies or substantial portions of the Software.
//
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND,
// EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES
// OF MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND
// NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT
// HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY,
// WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING
// FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR
// OTHER DEALINGS IN THE SOFTWARE.
//

#include "stdafx.h"
#include "Logging.h"
#include "ConsoleLogger.h"
#include <iostream>

namespace InterceptPP {

namespace Logging {

Event *
ConsoleLogger::NewEvent(const OString &eventType)
{
    return new Event(this, m_id++, eventType);
}

void
ConsoleLogger::SubmitEvent(Event *ev)
{
    PrintNode(ev);
}

void
ConsoleLogger::PrintNode(Node *node, int level)
{
    OString indentStr;
    for (int i = 0; i < level; i++)
        indentStr += "\t";

    cout << indentStr << node->GetName() << ":" << endl;

    if (node->GetFieldCount() > 0)
    {
        Logging::Node::FieldListConstIter iter, endIter = node->FieldsIterEnd();
        for (iter = node->FieldsIterBegin(); iter != endIter; iter++)
	    {
            cout << indentStr << "\t" << iter->first << ": " << iter->second << endl;
	    }

        cout << endl;
    }

    const OString &content = node->GetContent();
    if (content.size() > 0)
    {
        cout << indentStr << "\tContent:" << endl;

        if (!node->GetContentIsRaw())
        {
            cout << indentStr << "\t\t" << content << endl;
        }
        else
        {
            cout << indentStr << "\t\t" << "[raw content]" << endl;
        }
    }

    if (node->GetChildCount() > 0)
    {
        cout << indentStr << "\tChildren:" << endl;

		Logging::Node::ChildListConstIter iter, endIter = node->ChildrenIterEnd();

		for (iter = node->ChildrenIterBegin(); iter != endIter; iter++)
		{
			PrintNode(*iter, level + 2);
		}
    }

    cout << endl;
}

} // namespace Logging

} // namespace InterceptPP