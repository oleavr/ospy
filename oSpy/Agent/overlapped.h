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

class COverlappedOperation;

typedef void (*OperationCompleteHandler) (COverlappedOperation *operation);

class COverlappedManager
{
public:
	static void Init();
	static void TrackOperation(OVERLAPPED **overlapped, void *data, OperationCompleteHandler handler);

protected:
	static DWORD __stdcall MonitorThreadFunc(void *arg);

	static HANDLE m_opsChanged;

	static CRITICAL_SECTION m_opsCriticalSection;
	static OVector<COverlappedOperation *>::Type m_operations;
};

class COverlappedOperation : public BaseObject
{
public:
	COverlappedOperation(OVERLAPPED *clientOverlapped, void *data, OperationCompleteHandler handler);

	OVERLAPPED *GetClientOverlapped() { return m_clientOverlapped; }
	OVERLAPPED *GetRealOverlapped() { return m_realOverlapped; }
	void *GetData() { return m_data; }
	bool HasCompleted() { return (WaitForSingleObject(m_realOverlapped, 0) == WAIT_OBJECT_0); }
	void HandleCompletion();
	OperationCompleteHandler GetCompletionHandler() { return m_handler; }

protected:
	OVERLAPPED *m_clientOverlapped;
	OVERLAPPED *m_realOverlapped;
	void *m_data;
	bool m_completionHandled;
	OperationCompleteHandler m_handler;
};
