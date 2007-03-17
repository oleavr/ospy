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
#include "overlapped.h"

#define ENTER_OPS() EnterCriticalSection(&m_opsCriticalSection)
#define LEAVE_OPS() LeaveCriticalSection(&m_opsCriticalSection)

HANDLE COverlappedManager::m_opsChanged = 0;

CRITICAL_SECTION COverlappedManager::m_opsCriticalSection;
OVector<COverlappedOperation *>::Type COverlappedManager::m_operations;

void
COverlappedManager::Init()
{
	m_opsChanged = CreateEvent(NULL, FALSE, FALSE, NULL);

	InitializeCriticalSection(&m_opsCriticalSection);

	CreateThread(NULL, 0, MonitorThreadFunc, NULL, 0, NULL);
}

void
COverlappedManager::TrackOperation(OVERLAPPED **overlapped, void *data, OperationCompleteHandler handler)
{
	// FIXME: do garbage-collection here by having the client of this API provide
	//        a unique context id, i.e. socket handle + direction...
	COverlappedOperation *op = new COverlappedOperation(*overlapped, data, handler);
	*overlapped = op->GetRealOverlapped();

	ENTER_OPS();

	m_operations.push_back(op);
	SetEvent(m_opsChanged);

	LEAVE_OPS();
}

DWORD
COverlappedManager::MonitorThreadFunc(void *arg)
{
	while (true)
	{
		HANDLE *handles;
		COverlappedOperation **operations;
		int maxHandleCount, handleCount;

		// Make a list of operations not yet completed
		ENTER_OPS();

		maxHandleCount = 1 + m_operations.size();
		handles = (HANDLE *) sspy_malloc(sizeof(HANDLE) * maxHandleCount);
		operations = (COverlappedOperation **) sspy_malloc(sizeof(COverlappedOperation *) * maxHandleCount);

		handles[0] = m_opsChanged;
		operations[0] = NULL;

		handleCount = 1;

		for (int i = 0; i < m_operations.size(); i++)
		{
			COverlappedOperation *op = m_operations[i];
			if (!op->HasCompleted())
			{
				handles[handleCount] = op->GetRealOverlapped()->hEvent;
				operations[handleCount] = op;

				handleCount++;
			}
		}

		LEAVE_OPS();

		// Wait for events to be triggered
		bool operationsChanged = false;
		while (!operationsChanged)
		{
			DWORD result = WaitForMultipleObjects(handleCount, handles, FALSE, INFINITE);

			if (result >= WAIT_OBJECT_0 && result < WAIT_OBJECT_0 + handleCount)
			{
				operationsChanged = true;

				for (int i = result - WAIT_OBJECT_0; i < handleCount; i++)
				{
					if (i > 0 && WaitForSingleObject(handles[i], 0) == WAIT_OBJECT_0)
					{
						operations[i]->HandleCompletion();
					}
				}
			}
		}

		sspy_free(handles);
		sspy_free(operations);
	}

	return 0;
}

COverlappedOperation::COverlappedOperation(OVERLAPPED *clientOverlapped, void *data, OperationCompleteHandler handler)
	: m_clientOverlapped(clientOverlapped), m_data(data), m_completionHandled(false), m_handler(handler)
{
	m_realOverlapped = (OVERLAPPED *) sspy_malloc(sizeof(OVERLAPPED));
	memset(m_realOverlapped, 0, sizeof(OVERLAPPED));
	m_realOverlapped->hEvent = CreateEvent(NULL, TRUE, FALSE, NULL);
}

void
COverlappedOperation::HandleCompletion()
{
	if (!m_completionHandled)
	{
		m_completionHandled = true;
		m_handler(this);
	}
}
