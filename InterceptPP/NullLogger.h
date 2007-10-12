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

#pragma once

namespace InterceptPP {

namespace Logging {

class NullLogger : public Logger
{
public:
    NullLogger()
        : m_id(0)
    {}

    virtual Event *NewEvent(const OString &eventType) { return new Event(this, m_id++, eventType); }
    virtual void SubmitEvent(Event *ev) { delete ev; }

protected:
    unsigned int m_id;
};

} // namespace Logging

} // namespace InterceptPP
