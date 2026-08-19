# Phase 17 — Attendance & Leave

## Architectural principles

Attendance and Leave are independent business modules.
They reference employees by EmployeeId and do not become child
collections of the Employee aggregate.

Historical attendance, schedule assignments and leave records must
not be cascade-deleted with Employee records.

EmploymentPeriod defines whether an employee belongs to an active
employment period for attendance and leave eligibility.

Attendance raw events are preserved separately from calculated daily
attendance results.

Attendance event timestamps are stored in UTC. Business WorkDate is
stored explicitly and must not be reconstructed from UTC timestamps.

Work schedule expectations used for historical attendance are
snapshotted so later schedule changes do not rewrite historical
attendance meaning.

Leave request status transitions are historical and append-only.

Approved leave is an input to attendance calculation. Attendance does
not modify leave requests, and leave approval does not directly rewrite
attendance events.

Audit records remain metadata-only and must not contain sensitive HR
payloads.