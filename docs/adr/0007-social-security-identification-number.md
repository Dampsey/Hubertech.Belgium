# 7. Social security identification number

- Status: Accepted
- Date: 2026-10-06

## Context

The social security identification number (NISS in French, INSZ in Dutch) identifies a person in payroll, human resources and health applications. It is the national register number of a person registered in the National Register, or the BIS number assigned by the Crossroads Bank for Social Security to a person who is not. Both have eleven digits: a date of birth `YYMMDD`, a serial number, and two check digits.

Unlike the other identifiers, it is personal data, and its use is regulated: article 8 of the law of 8 August 1983 organising a National Register of natural persons restricts the use of the national register number.

The official texts could not be read first hand from the development environment; they were read through search engine excerpts, and cross-checked with python-stdnum, read from its source:

- the Royal Decree of 3 April 1984 and the instruction TI000 of the National Register, for the national register number: check digits equal to 97 minus the first nine digits modulo 97, these nine digits being preceded by a 2 for a person born from 2000;
- the Royal Decree of 8 February 1991, article 2, for the BIS number: the month of birth is increased by 40 when the sex of the person is known when the number is assigned, by 20 otherwise.

## Decisions

- **One type, `SocialSecurityIdentificationNumber`**, named after the official English term (SSIN), for both kinds of number. `Kind` tells them apart from the month of birth. The messages talk about a "national register or BIS number", the words people find on their documents.
- **The check digits tell the century.** 2,000,000,000 is 68 modulo 97, not 0, so the two computations never accept the same number: the one that matches gives the century, which `BirthDate` needs.
- **A birth after the current year is impossible.** Accepting both computations lets typos through: in a simulation on 20,000 random numbers, 0.41% of the single-digit typos and 0.95% of the swaps of adjacent digits turned a valid number into another valid one, against none for the other identifiers. Rejecting the numbers whose check digits only match a birth from 2000 after the current year brings these rates down to 0.21% and 0.63%. Validation therefore depends on the current UTC year. A property-based test checks that a typo that goes undetected always changes the century, which is what modulo 97, 97 being prime, guarantees.
- **The month is checked, the day is not.** A month other than 00 to 12, 20 to 32 or 40 to 52 is reported with a new code, `InvalidBirthDate`. The day is not checked, because the register writes zeros, or other digits, in the date of birth when it is unknown or when the serial numbers of a day run out (instruction TI000, as described by python-stdnum).
- **`Expected` is `null`** for an invalid number: the expected check digits depend on the century, which the number does not carry.
- **`BirthDate` is nullable.** It is `null` when the encoded month or day is zero, or when the digits are not a date. It is the date known when the number was assigned, not an authoritative date of birth.
- **The sex is not exposed.** The parity of the serial number encodes the sex registered when the number was assigned, but it is unknown for a BIS number increased by 20, and an application that needs the sex of a person should ask for it or read it from the register rather than infer it from an identifier. It can be added later without breaking anything.
- **`ToString()` masks the number**, as `**.**.**-***.28`: data protection by default (GDPR, article 25). Logging a value, or showing it in a debugger, does not leak it. The date of birth is masked too, being personal data as well. The full number needs an explicit format, `D` or `N`.
- **JSON holds the full number**, since programs that receive it need it. The converter's documentation warns about logging such JSON.

## Consequences

- `ToString()` behaves differently from the other identifiers. This is documented on the type and in the README.
- The detection of typos is weaker than for the other identifiers, and the README says so.
- A number valid today stays valid; a number rejected today because of its year of birth becomes valid in that year.
- The library checks the syntax only. Whether an application may process the number is a legal question that it does not answer.
