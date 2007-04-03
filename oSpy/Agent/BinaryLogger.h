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

#pragma once

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
    unsigned int m_id;
    volatile bool m_running;
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