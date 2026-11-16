# SumScription — Design Direction

## Character

SumScription is a modern desktop calculator with a calm, translucent liquid-glass interface. Ordinary calculation stays restrained and credible; the fictional paywall deliberately breaks that tone as an exaggerated joke.

## Interface

- A deep-blue gradient window with lightweight internal glass surfaces and no expensive full-window blur effects.
- One large glass surface combines the expression, result, and history.
- Four familiar keypad columns; operators form a continuous orange axis.
- SF Pro or an appropriate system font, with large lightweight results and compact labels.
- Dark and light themes use the same hierarchy with adapted semantic colors.
- Visible hover, press, and keyboard-focus states; keyboard and numeric keypad input remain fully supported.
- Reduced motion is available through `SUMSCRIPTION_REDUCED_MOTION=1`.

## Parody paywalls

- The result paywall does not appear at launch. Each app run randomly selects the fourth through seventh successful evaluation as its trigger; older persisted totals never cause an immediate first-calculation paywall.
- Errors and empty evaluations do not consume the free quota.
- The result paywall uses a compact monthly/annual switch. Monthly offers are one calculation for €4.99 and Monthly Ultra for €74.99. Annual offers are Standard for €499 without percentage credits and Premium for €699 with 100 percentage calculations. The single-calculation purchase reveals only the currently locked result; the next paid result is locked again.
- The percent key is a separate premium feature. Without percentage credits it opens an offer for 1 calculation at €9.99, 10 at €69.99, or 100 at €599.
- After the first result paywall appears, a glowing diamond shop icon at the left edge replaces the conventional reopen button and deliberately echoes microtransaction-heavy mobile games without covering right-aligned calculator input. It hides while the paywall or checkout is open so it never competes with an offer.
- Fictional percentage purchases last only for the current app session. The pending percentage operation completes immediately and consumes one credit.
- Every overlay clearly states that the purchase is fictional and never collects payment details.
- Rejecting an offer requires three differently worded refusals before calculator input returns.
- The refusal button evades pointer input across the full window and cannot be activated with a mouse. It jumps outside a protected cursor radius with a short cooldown, preventing recursive pointer-enter animation storms. Clicking the scrim only makes it flee again. Keyboard focus, Enter, Space, and Escape remain available as an accessibility exit.
- A fictional purchase triggers 48 large, colorful confetti pieces and a prominent confirmation banner; reduced motion receives a longer static confirmation instead.
- Result-unlock purchases only last for the current app session. Restarting the app removes all fictional purchases and starts a fresh randomized quota while preserving the lifetime evaluation counter.
- Magenta, violet, orange, cyan, and warning yellow make the paywall much louder than the calculator itself.
- The monthly/annual switch uses cyan with dark plum text for its active segment instead of warning yellow. The evasive refusal control is a compact dark-violet glass button, keeping it legible without visually competing with the offer cards.

## Size and motion

- Default window: 420 × 700; minimum size: 390 × 650; freely resizable.
- Keys are 60 points tall and grow to 66 points above a 760-point window height.
- Large glass surfaces use generous radii; small controls use noticeably tighter radii. The paywall avoids full-tree opacity compositing, system backdrop blur, and GPU blur shadows.
- Input responds immediately. Results and overlays use short restrained motion; only refusals and fictional purchase celebrations become deliberately theatrical.
