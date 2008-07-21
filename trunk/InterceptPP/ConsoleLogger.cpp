//
// Copyright (c) 2007 Ole André Vadla Ravnås <oleavr@gmail.com>
//
// This library is free software: you can redistribute it and/or modify
// it under the terms of the GNU General Public License as published by
// the Free Software Foundation, either version 3 of the License, or
// (at your option) any later version.
//
// This library is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
// GNU General Public License for more details.
//
// You should have received a copy of the GNU General Public License
// along with this program.  If not, see <http://www.gnu.org/licenses/>.
//

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
    delete ev;
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
