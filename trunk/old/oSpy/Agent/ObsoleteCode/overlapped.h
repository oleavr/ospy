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
