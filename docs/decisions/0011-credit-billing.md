# 0011. Mock credit billing semantics

## Context

Consumers have a virtual credit balance; successful requests deduct credits according to the tier.
No real payments.

## Decision

- **One balance per consumer** (`Users.CreditBalance`), shared across all their keys and APIs.
- **Charged on success only.** A request is charged the tier's *credit cost per request* when the
  upstream answers with a status below 400. Failed and rejected requests cost nothing.
- **Pre-check, then charge.** `CreditGate` refuses a request up front with `402 insufficient_credits`
  when the balance is below the cost. The charge itself happens after the response, in
  `RequestSettlement`.
- **Atomic balance changes.** Charging is a single statement:
  `UPDATE Users SET CreditBalance = CreditBalance - @cost WHERE Id = @id AND CreditBalance >= @cost`.
  Top-ups are a single `+ @amount` statement. The arithmetic is done by the database, so concurrent
  requests cannot lose updates or push a balance below zero.
- **Top-up is a mock purchase:** the consumer clicks a button, credits are added (1 to 10,000 per
  top-up) and a `CreditTransaction` row records it.
- **Welcome bonus:** a newly registered consumer gets 100 credits (`CreditPolicy.WelcomeBonus`), so a
  fresh account can make requests immediately.
- A tier with cost 0 is free and never reads the balance.
- The credits charged are stored on each usage record; "credits consumed" in analytics is their sum.

## Alternatives considered

- **Reserve credits before forwarding, refund on failure.** Never lets a request through unpaid, but
  needs two writes per request and a refund path.
- **Balance in Redis, flushed to MySQL periodically.** Removes a database write from the hot path at
  the cost of a second source of truth for money-like data.
- **A balance per API or per owner.** More realistic for a marketplace; the PRD describes one balance
  per consumer.
- **Read-modify-write through the entity.** A top-up could overwrite a charge made in between.

## Consequences

- **A burst can get slightly more than was paid for.** Several concurrent requests can all pass the
  pre-check when the balance covers only one; each is forwarded, but only those whose atomic charge
  succeeds are billed. The balance never goes negative; the consumer gets a few free calls. Usage
  records show such calls as successful with zero credits charged.
- Two database round trips per paid request (read balance, charge). Acceptable at demo scale; it is
  the first thing to move if latency matters.
- Credits are not owed to anyone: owners do not earn what consumers spend.
