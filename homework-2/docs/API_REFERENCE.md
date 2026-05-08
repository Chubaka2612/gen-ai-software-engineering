# API Reference

Base URL: `http://localhost:5000`

## Endpoints

| Method | Route | Description |
|--------|-------|-------------|
| POST | /api/tickets | Create a new support ticket |
| GET | /api/tickets | List tickets with optional filters |
| GET | /api/tickets/{id} | Get a ticket by ID |
| PUT | /api/tickets/{id} | Update a ticket |
| DELETE | /api/tickets/{id} | Delete a ticket |
| POST | /api/tickets/import | Bulk-import tickets from a file |
| POST | /api/tickets/{id}/auto-classify | Auto-classify a ticket by keyword analysis |

---

## POST /api/tickets

### Description

Creates a new support ticket and returns it with a `Location` header pointing to the new resource.

### Request Body

```json
{
  "customerId":    "string (required)",
  "customerEmail": "string (required, valid email)",
  "customerName":  "string (required)",
  "subject":       "string (required, 1–200 chars)",
  "description":   "string (required, 10–2000 chars)",
  "category":      "AccountAccess | TechnicalIssue | BillingQuestion | FeatureRequest | BugReport | Other",
  "priority":      "Urgent | High | Medium | Low",
  "source":        "WebForm | Email | Api | Chat | Phone",
  "deviceType":    "Desktop | Mobile | Tablet",
  "tags":          ["string"],
  "assignedTo":    "string (optional)",
  "browser":       "string (optional)",
  "autoClassify":  false
}
```

Setting `autoClassify: true` runs keyword analysis immediately after creation and overwrites `category` and `priority` in the response.

### Response — Success

**201 Created** — `Location: /api/tickets/{id}`

```json
{
  "id":            "3fa85f64-5717-4562-b3fc-2c963f66afa6",
  "customerId":    "cust-001",
  "customerEmail": "alice@example.com",
  "customerName":  "Alice Smith",
  "subject":       "Cannot log in to my account",
  "description":   "I have been unable to log in since yesterday morning.",
  "category":      "AccountAccess",
  "priority":      "Medium",
  "status":        "New",
  "createdAt":     "2026-05-08T12:00:00Z",
  "updatedAt":     "2026-05-08T12:00:00Z",
  "resolvedAt":    null,
  "assignedTo":    null,
  "tags":          [],
  "source":        "Api",
  "browser":       null,
  "deviceType":    "Desktop"
}
```

### Response — Errors

| Error Code | HTTP Status | When It Occurs |
|------------|-------------|----------------|
| `Validation.Failed` | 400 | A required field is missing or a value violates a constraint |

### Example

```bash
curl -s -X POST http://localhost:5000/api/tickets \
  -H "Content-Type: application/json" \
  -d '{
    "customerId":    "cust-001",
    "customerEmail": "alice@example.com",
    "customerName":  "Alice Smith",
    "subject":       "Cannot log in to my account",
    "description":   "I have been unable to log in since yesterday morning.",
    "category":      "AccountAccess",
    "priority":      "Medium",
    "source":        "Api",
    "deviceType":    "Desktop",
    "tags":          []
  }'
```

---

## GET /api/tickets/{id}

### Description

Returns a single ticket by its GUID.

### Response — Success

**200 OK** — full ticket object (same shape as POST 201 response above).

### Response — Errors

| Error Code | HTTP Status | When It Occurs |
|------------|-------------|----------------|
| `Ticket.NotFound` | 404 | No ticket exists with the given ID |

### Example

```bash
curl -s http://localhost:5000/api/tickets/3fa85f64-5717-4562-b3fc-2c963f66afa6
```

---

## GET /api/tickets

### Description

Returns a paginated, optionally filtered list of tickets.

### Query Parameters

| Parameter | Type | Default | Description |
|-----------|------|---------|-------------|
| `category` | string | — | Filter by category enum value |
| `priority` | string | — | Filter by priority enum value |
| `status` | string | — | Filter by status enum value |
| `assignedTo` | string | — | Filter by assigned agent name |
| `page` | integer | 1 | Page number (1-based) |
| `pageSize` | integer | 20 | Items per page |

### Response — Success

**200 OK**

```json
{
  "items":      [ { /* ticket list item */ } ],
  "totalCount": 42,
  "page":       1,
  "pageSize":   20
}
```

### Response — Errors

This endpoint always returns 200. An empty `items` array with `totalCount: 0` means no tickets match the filters.

### Example

```bash
curl -s "http://localhost:5000/api/tickets?category=BugReport&priority=High&page=1&pageSize=10"
```

---

## PUT /api/tickets/{id}

### Description

Partially updates a ticket. All fields are optional; omitted fields retain their current values. To advance the ticket's status, include the `status` field — only forward transitions are accepted.

### Status Transition Rules

```
New → InProgress → WaitingCustomer → Resolved → Closed
```

Any other transition returns `422 Unprocessable Entity`.

### Request Body

```json
{
  "subject":     "string (optional, 1–200 chars)",
  "description": "string (optional, 10–2000 chars)",
  "category":    "AccountAccess | TechnicalIssue | BillingQuestion | FeatureRequest | BugReport | Other (optional)",
  "priority":    "Urgent | High | Medium | Low (optional)",
  "status":      "New | InProgress | WaitingCustomer | Resolved | Closed (optional)",
  "assignedTo":  "string (optional)",
  "tags":        ["string"] ,
  "browser":     "string (optional)",
  "deviceType":  "Desktop | Mobile | Tablet (optional)"
}
```

### Response — Success

**200 OK** — updated ticket object (same shape as POST 201 response).

### Response — Errors

| Error Code | HTTP Status | When It Occurs |
|------------|-------------|----------------|
| `Validation.Failed` | 400 | A supplied field value violates a constraint |
| `Ticket.NotFound` | 404 | No ticket exists with the given ID |
| `Ticket.InvalidStatus` | 422 | The requested status transition is not allowed |

### Example

```bash
curl -s -X PUT http://localhost:5000/api/tickets/3fa85f64-5717-4562-b3fc-2c963f66afa6 \
  -H "Content-Type: application/json" \
  -d '{"status": "InProgress"}'
```

---

## DELETE /api/tickets/{id}

### Description

Permanently deletes a ticket. Tickets in `Resolved` or `Closed` status cannot be deleted.

### Response — Success

**204 No Content**

### Response — Errors

| Error Code | HTTP Status | When It Occurs |
|------------|-------------|----------------|
| `Ticket.NotFound` | 404 | No ticket exists with the given ID |
| `Ticket.InvalidStatus` | 422 | The ticket is in `Resolved` or `Closed` status |

### Example

```bash
curl -s -X DELETE http://localhost:5000/api/tickets/3fa85f64-5717-4562-b3fc-2c963f66afa6
```

---

## POST /api/tickets/import

### Description

Bulk-imports tickets from an uploaded file. Accepted formats: CSV, JSON, XML. Maximum file size: 10 MB. Rows that fail validation are reported individually in the `errors` array; valid rows are persisted.

### Request Body

`Content-Type: multipart/form-data`

| Field | Type | Description |
|-------|------|-------------|
| `file` | file | CSV, JSON, or XML file containing ticket records |

**CSV format** — header row required; columns: `customerId`, `customerEmail`, `customerName`, `subject`, `description`, `category`, `priority`. Optional columns: `source`, `deviceType`, `assignedTo`, `browser`, `tags`.

**JSON format** — array of objects with the same fields as CSV.

**XML format** — root element `<tickets>`, each ticket in a `<ticket>` child element.

### Response — Success

**200 OK**

```json
{
  "total":      3,
  "successful": 2,
  "failed":     1,
  "errors": [
    { "rowNumber": 3, "message": "Subject is required." }
  ]
}
```

### Response — Errors

| Error Code | HTTP Status | When It Occurs |
|------------|-------------|----------------|
| `Validation.Failed` | 400 | No file provided, file is empty, or format is unsupported |

### Example

```bash
curl -s -X POST http://localhost:5000/api/tickets/import \
  -F "file=@tickets.json;type=application/json"
```

---

## POST /api/tickets/{id}/auto-classify

### Description

Analyses the ticket's subject and description using keyword matching and updates its `category` and `priority`. An optional request body allows overriding the classifier's decision.

### Request Body (optional)

```json
{
  "categoryOverride": "AccountAccess | TechnicalIssue | BillingQuestion | FeatureRequest | BugReport | Other (optional)",
  "priorityOverride": "Urgent | High | Medium | Low (optional)"
}
```

### Response — Success

**200 OK**

```json
{
  "category":      "AccountAccess",
  "priority":      "Medium",
  "confidence":    0.83,
  "reasoning":     "Matched keywords: login, password",
  "keywordsFound": ["login", "password"]
}
```

`confidence` is a value between `0.0` (no keywords matched) and `1.0` (maximum match score).

### Response — Errors

| Error Code | HTTP Status | When It Occurs |
|------------|-------------|----------------|
| `Ticket.NotFound` | 404 | No ticket exists with the given ID |

### Example

```bash
curl -s -X POST http://localhost:5000/api/tickets/3fa85f64-5717-4562-b3fc-2c963f66afa6/auto-classify \
  -H "Content-Type: application/json" \
  -d '{}'
```

---

## Error Response Format

All non-200 error responses from this API use the following JSON envelope:

```json
{
  "code":    "Ticket.NotFound",
  "message": "Ticket with ID 3fa85f64-5717-4562-b3fc-2c963f66afa6 was not found."
}
```

Validation errors from the `POST /api/tickets` and `PUT /api/tickets/{id}` endpoints use ASP.NET Core's standard problem-details format:

```json
{
  "errors": {
    "Subject": ["Subject is required."]
  },
  "status": 400,
  "title":  "One or more validation errors occurred."
}
```

---

## Error Code Catalogue

| Code | HTTP Status | Meaning |
|------|-------------|---------|
| `Validation.Failed` | 400 | The request body or file fails validation |
| `Ticket.NotFound` | 404 | No ticket exists with the given ID |
| `Ticket.Duplicate` | 409 | A ticket with the same generated ID already exists |
| `Ticket.InvalidStatus` | 422 | The requested status transition is not permitted, or the ticket cannot be deleted in its current status |
| `General.Unexpected` | 500 | An unhandled internal error occurred |
