# Postman Collection

End-to-end tests for the Library Management API: **53 requests**, each with automated checks on the status code, the response body and, for failures, the error `code`.

| File | Purpose |
|---|---|
| `UobLibrary.postman_collection.json` | The requests and their test scripts |
| `UobLibrary-Local.postman_environment.json` | `baseUrl` = `http://localhost:5080` |

## How to run

1. Start the API from the repository root:
   ```bash
   dotnet run --project src/LibraryManagement.Api
   ```
2. In Postman, click **Import** and select both JSON files.
3. Choose the **UOB Library API - Local** environment (top-right drop-down).
4. Right-click the collection, choose **Run collection**, keep the default order and click **Run**.

Expected result: every test passes.

You can also send requests one at a time. Run **0. Setup → Health check** first, because it creates the `runId` used to make test data unique. After that, run the requests in order: later requests use ids saved by earlier ones.

## What it covers

| Folder | What it tests |
|---|---|
| **0. Setup** | `GET /health`; generates a fresh `runId` |
| **1. Books** | Create (the ISBN is sent with a hyphen to prove it's normalised), get, search with paging, update stock, filter by genre |
| **2. Members** | Register Standard and Premium members (tier limits checked), email lower-casing, get, search, update |
| **3. Loans: happy path** | Borrow (14-day vs 30-day due dates), stock goes down and back up, active/returned filters, member history, on-time return with no fine |
| **4. Business rules and errors** | `BOOK_UNAVAILABLE`, `BOOK_ALREADY_BORROWED_BY_MEMBER`, `MEMBER_HAS_ACTIVE_LOANS`, `BOOK_HAS_ACTIVE_LOANS`, `LOAN_LIMIT_REACHED`, `LOAN_ALREADY_RETURNED`, `MEMBER_INACTIVE`, `DUPLICATE_ISBN`, `DUPLICATE_EMAIL`, `VALIDATION_ERROR`, model-validation 400s, and 404 `NOT_FOUND` |
| **5. Cleanup and soft delete** | Returns the remaining loans, retires a book and deactivates a member, then shows that both still exist (soft delete) |

## Notes

- **Repeatable:** every run creates its own books and members, with unique ISBNs and emails based on `runId`. It doesn't depend on the seed data, and it doesn't clash with earlier runs.
- **Overdue and fines:** a loan can't be made overdue from Postman without waiting 14+ days. The collection only checks that the `status=Overdue` filter works. The overdue and fine rules are covered by the automated tests (`dotnet test`), which control time with `TestClock`.
- **Different port:** if the API runs elsewhere (e.g. `https://localhost:7080`), change `baseUrl` in the environment.
