//
// Copyright (c) 2007 Ole André Vadla Ravnås <oleavr@gmail.com>
//
// This program is free software: you can redistribute it and/or modify
// it under the terms of the GNU General Public License as published by
// the Free Software Foundation, either version 3 of the License, or
// (at your option) any later version.
//
// This program is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
// GNU General Public License for more details.
//
// You should have received a copy of the GNU General Public License
// along with this program.  If not, see <http://www.gnu.org/licenses/>.
//

#pragma once

namespace oSpy {

class Agent;

class BinaryLogger : public Logging::Logger
{
public:
    BinaryLogger(Agent *agent, const OWString &filename);
    virtual ~BinaryLogger();

    virtual Logging::Event *NewEvent(const OString &eventType);
    virtual void SubmitEvent(Logging::Event *ev);

protected:
    Agent *m_agent;

    HANDLE m_handle;
    HANDLE m_destroyEvent;
    HANDLE m_loggingThreadHandle;
    SLIST_HEADER m_pendingEvents;

    void FlushPending();

    static DWORD WINAPI LoggingThreadFuncWrapper(LPVOID param);
    void LoggingThreadFunc();
};

class BinarySerializer : public BaseObject
{
public:
    const OString &GetData() { return m_buf; }

    void AppendNode(Logging::Node *node);
    void AppendString(const OString &s);
    void AppendDWord(DWORD dw);

protected:
    OString m_buf;
};

} // namespace oSpy
