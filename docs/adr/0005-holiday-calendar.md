# 5. Holiday calendar

- Status: Accepted
- Date: 2026-10-05

## Context

Invoices, payment terms and administrative deadlines are counted in business days. Belgium has ten legal holidays, days off for every worker, set by the Royal Decree of 18 April 1974. Other days are off for some employers only: the day of each community for its public services, and three more days for the federal public services (Royal Decree of 19 November 1998, article 14). Three legal holidays move every year with Easter.

## Options

1. **Tables of dates**, one per year. Simple to read, but they must be maintained and they stop at some year.
2. **Rules**: fixed days, and offsets from Easter Sunday computed by an algorithm.
3. **An existing library** such as Nager.Date. A dependency, which this package refuses, and a general-purpose model without the Belgian sets of days off.

## Decision

Option 2.

- **Easter** is computed with the anonymous Gregorian algorithm (Meeus, Jones, Butcher). Its dates from 2020 to 2035 are checked against python-dateutil, and a property-based test checks that it is a Sunday between 22 March and 25 April for every year from 1 to 9999.
- **Sets of days off** are a `[Flags]` enum, `HolidaySet`, that callers combine. The default is `Legal` only. Sets are independent: `FederalPublicService` holds the three extra days, and a federal employer asks for `Legal | FederalPublicService`. Unknown flags are rejected.
- **The rules in force today are applied to every year** from 1 to 9999. The calendar does not model the history of Belgian holidays.
- **Two holidays can fall on the same date**: 15 November is both the day of the German-speaking Community and the King's Feast of the federal public services. Both are returned, ordered by date then by kind.
- **A business day** is a Monday to Friday that is not a holiday of the requested set.
- **`AddBusinessDays`** does not count the starting date, returns it unchanged for 0 days, and moves backward for a negative number.
- **`CountBusinessDays`** never counts the start and always counts the end, in both directions: (start, end] forward, [end, start) backward. It is then exactly the inverse of `AddBusinessDays`, which a property-based test checks. A symmetric definition was tried first; the same test found that it broke this inverse when starting from a weekend. These are also the bounds Belgian law uses for a deadline: from the day after the triggering event, the last day included (Judicial Code, articles 52 and 53).
- **Names** are localized in English, French and Dutch with the strategy of ADR 0003, in `HolidayNames.resx`.
- **`BelgianHoliday` and `BelgianCalendar` are prefixed**, because `Holiday` is a common type in human resources applications and `Calendar` already exists in `System.Globalization`. `HolidayKind` and `HolidaySet` are specific enough to stay unprefixed.

## Not modelled

- **Replacement days.** A holiday that falls on a Saturday or a Sunday stays there. The replacement day is set by each employer under labour law, and the federal public services compensate them between 27 and 31 December.
- **Half days**, such as the afternoon of 22 July for the federal public services.
- **Regional days off and school holidays.**

## Consequences

- No yearly maintenance and no upper year limit other than `DateOnly`'s.
- `CountBusinessDays(a, b)` is not the opposite of `CountBusinessDays(b, a)` when one of the bounds is not a business day: the sum of both is 1, -1 or 0 depending on which bound is a business day. This is documented and tested.
- `IsHoliday`, `IsBusinessDay`, `AddBusinessDays` and `CountBusinessDays` do not allocate. Counting is linear in the number of days, which is enough for the ranges of invoices and deadlines; the benchmarks of phase 7 will tell whether larger ranges need better.
- A change in the law, such as a new legal holiday, needs a new version of the package.
