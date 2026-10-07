---
name: EventTix Hub
description: Билети за кино, концерти и театър. A Bulgarian афиш set in one Cyrillic type family and three spot inks.
colors:
  paper: "#F3F2EE"
  sheet: "#FBFAF7"
  ink: "#171614"
  ink-muted: "#57554F"
  rule: "#D5D3CC"
  field-border: "#85827A"
  film: "#2D3AB4"
  theatre: "#C3361C"
  concert: "#F1B20A"
  on-film: "#FAF9F6"
  on-theatre: "#FAF9F6"
  on-concert: "#171614"
  danger: "#B42318"
  success: "#1D7A3E"
  seat-held: "#8F8C84"
  seat-sold: "#C8C6BF"
  night: "#121211"
  night-sheet: "#1C1B19"
  night-ink: "#EEECE7"
  night-ink-muted: "#A9A69E"
  night-rule: "#383733"
  night-field-border: "#7B786F"
  night-film: "#3B49C8"
  night-concert: "#E3A40B"
  night-danger: "#FF8A7A"
  night-success: "#6FCF8E"
  night-seat-free: "#2A2926"
  night-seat-held: "#8A877F"
  night-seat-sold: "#45433F"
typography:
  display:
    fontFamily: "'Sofia Sans Extra Condensed', 'Arial Narrow', sans-serif"
    fontSize: "clamp(2.75rem, 1.6rem + 5vw, 5.5rem)"
    fontWeight: 800
    lineHeight: 0.9
    letterSpacing: "0"
  headline:
    fontFamily: "'Sofia Sans Extra Condensed', 'Arial Narrow', sans-serif"
    fontSize: "2.25rem"
    fontWeight: 800
    lineHeight: 1
    letterSpacing: "0"
  figure:
    fontFamily: "'Sofia Sans Extra Condensed', 'Arial Narrow', sans-serif"
    fontSize: "3rem"
    fontWeight: 800
    lineHeight: 0.85
    fontFeature: "'tnum' 1"
  title:
    fontFamily: "'Sofia Sans', system-ui, 'Segoe UI', Arial, sans-serif"
    fontSize: "1.25rem"
    fontWeight: 700
    lineHeight: 1.25
  body:
    fontFamily: "'Sofia Sans', system-ui, 'Segoe UI', Arial, sans-serif"
    fontSize: "1rem"
    fontWeight: 400
    lineHeight: 1.5
  label:
    fontFamily: "'Sofia Sans', system-ui, 'Segoe UI', Arial, sans-serif"
    fontSize: "0.8125rem"
    fontWeight: 700
    lineHeight: 1.35
    letterSpacing: "0.05em"
rounded:
  none: "0"
  sm: "2px"
  seat: "10px 10px 4px 4px"
spacing:
  xs: "4px"
  sm: "8px"
  md: "16px"
  lg: "24px"
  xl: "48px"
components:
  button-primary:
    backgroundColor: "{colors.ink}"
    textColor: "{colors.sheet}"
    typography: "{typography.body}"
    rounded: "{rounded.sm}"
    padding: "10px 20px"
    height: "44px"
  button-primary-hover:
    backgroundColor: "{colors.ink-muted}"
    textColor: "{colors.sheet}"
  button-secondary:
    backgroundColor: "{colors.sheet}"
    textColor: "{colors.ink}"
    rounded: "{rounded.sm}"
    padding: "10px 20px"
    height: "44px"
  input:
    backgroundColor: "{colors.sheet}"
    textColor: "{colors.ink}"
    rounded: "{rounded.sm}"
    padding: "10px 12px"
    height: "44px"
  badge-type:
    textColor: "{colors.ink}"
    typography: "{typography.label}"
    padding: "0 0 2px"
  hold-badge:
    backgroundColor: "{colors.sheet}"
    textColor: "{colors.ink}"
    typography: "{typography.label}"
    rounded: "{rounded.sm}"
    padding: "6px 10px"
  hold-badge-urgent:
    backgroundColor: "{colors.danger}"
    textColor: "{colors.sheet}"
---

# Design System: EventTix Hub

## Overview

**Creative North Star: "Печатницата" (the theatre's own print shop)**

Every screen is one of the printed things a Bulgarian theatre house hands you: the **афиш** on the street, the **програма** with the dates, the **схема на залата** at the box office, the **билет**, and the **покана** (invitation). All of them are set in one Cyrillic type family and printed with three spot inks, one per art form:

- **кино** (film): ultramarine
- **концерти** (concerts): saffron
- **театър** (theatre): vermilion

The ground is paper, the text is ink, and the colour belongs to the event. This replaces the stock `dotnet new mvc` + Bootstrap look. Bootstrap 5.3 stays underneath as the component engine, restyled through its CSS variables and `site.css`.

The world reaches each surface as far as the job allows:

- **Афиш and event pages (Persuade).** The world owns the page. Posters are the content, and type is set big.
- **Booking flow, profile and admin (Operate).** The world lends only type, palette, density and one signature move per surface:
  - the hall plan
  - the hold badge
  - the invitation ticket
  - the programme board in admin

  Controls stay standard Bootstrap controls in the house style.

The world is flat. It uses no shadows, no gradients, no glass, no glow, and no fake paper grain. Depth comes from paper layers and printed rules.

**Key Characteristics:**

- Paper ground, ink type, three spot inks (ultramarine film, saffron concert, vermilion theatre).
- One family, Sofia Sans (Lettersoup, Bulgarian Cyrillic forms), in two widths. **Extra Condensed** is for what you look at; **normal width** is for what you read and operate.
- Every event has a poster. When no image was uploaded, the poster is generated from the title, type, date and venue.
- Square printed corners. Only seats get a rounded "chair back".
- Seat states read by fill, pattern and outline weight, never by hue alone.
- Light and dark themes are equal citizens. Print is always light.

## Colors

The palette is three spot inks on paper. Neutrals are ink and paper; colour belongs only to the art forms and to state.

### Primary

The three type inks. Each is the colour of one art form and appears only as a field with its paired text.

- **Ultramarine Film Ink:**
  - Light theme `{colors.film}` with `{colors.on-film}` text (8.4:1). Dark theme `{colors.night-film}` (6.7:1).
  - Used for film posters, the ФИЛМ badge, the screen arc on the hall plan, selected seats at a screening, and film bars in admin.
- **Vermilion Theatre Ink:**
  - `{colors.theatre}` with `{colors.on-theatre}` text (5.2:1) in both themes.
  - Used for theatre posters, the ТЕАТЪР badge, the stage block at plays, selected seats at a play, and theatre bars in admin.
- **Saffron Concert Ink:**
  - Light theme `{colors.concert}` with `{colors.on-concert}` (ink) text (9.6:1). Dark theme `{colors.night-concert}`, deepened to cut glare (8.3:1).
  - Used for concert posters, the КОНЦЕРТ badge, the stage block at concerts, selected seats at a concert, and concert bars in admin.

### Neutral

- **Newsprint Paper** (`{colors.paper}`, dark `{colors.night}`): the page ground, the wall the posters hang on.
- **Fresh Sheet** (`{colors.sheet}`, dark `{colors.night-sheet}`): raised paper for forms, panels, cart and summary, tables and tickets.
- **Press Ink** (`{colors.ink}`, dark `{colors.night-ink}`): body text, headings, primary buttons, rules and the focus ring (16:1 on paper).
- **Worn Ink** (`{colors.ink-muted}`, dark `{colors.night-ink-muted}`): secondary text, metadata and seat outlines (6.6:1 on paper).
- **Hairline** (`{colors.rule}`, dark `{colors.night-rule}`): decorative row dividers only. It is never the only boundary of a control.
- **Field Edge** (`{colors.field-border}`, dark `{colors.night-field-border}`): input and select borders (at least 3.4:1, which meets the 3:1 rule for UI boundaries).

### State

- **Danger** (`{colors.danger}`, dark `{colors.night-danger}`):
  - Used for error text, invalid field borders, destructive confirmations, and the hold badge under 2:00.
  - It replaces Bootstrap's `#dc3545`, whose dark-mode text measured 3.41:1.
- **Success** (`{colors.success}`, dark `{colors.night-success}`): payment success and "Платено" states.
- **Seat held** (`{colors.seat-held}`) and **seat sold** (`{colors.seat-sold}`): the hatch and the fill for unavailable seats. Dark variants: `{colors.night-seat-held}` and `{colors.night-seat-sold}`. A free seat in dark mode is `{colors.night-seat-free}`, never white.
- **VIP zone band:** the event's ink at 14 % over paper (22 % over night), mixed with `color-mix()` at runtime. It is not a stored token.

### Theme mapping (Bootstrap 5.3)

`site.css` maps tokens onto Bootstrap variables under `:root, [data-bs-theme=light]` and again under `[data-bs-theme=dark]`.

| Bootstrap variables | Token |
|---|---|
| `--bs-body-bg` | paper |
| `--bs-body-color`, `--bs-emphasis-color` | ink |
| `--bs-secondary-color` | ink-muted |
| `--bs-border-color` | rule |
| `--bs-link-color`, `--bs-link-hover-color` | ink |
| `--bs-primary` and `-rgb` | ink |
| `--bs-danger`, `-rgb`, `--bs-danger-text-emphasis` | danger |
| `--bs-success` and `-rgb` | success |
| `--bs-border-radius*` | `{rounded.sm}` |
| `--bs-font-sans-serif` | Sofia Sans stack |

Bootstrap compiles literal blues into components. The selectors that need explicit overrides in `site.css`:

- `.btn-primary`, `.btn-outline-*`
- `.pagination`, `.nav-pills`, `.nav-tabs`, `.progress`
- `.form-control:focus`, `.form-select:focus`, `.form-check-input:checked`
- `.alert-*`, `.badge`

### Named Rules

**The Three Inks Rule.** Ultramarine, saffron and vermilion belong to film, concert and theatre. Nothing else on the site uses them: not links, not buttons, not decoration.

**The Field Rule.** A type ink is always a field carrying its paired text. Type ink never colours text on paper; saffron would fail at 1.7:1.

**The Ink Rule.** Actions are ink. No `#0d6efd`, no blue focus glow, anywhere.

## Typography

**Display font:** Sofia Sans Extra Condensed (fallback: Arial Narrow, sans-serif)
**Body font:** Sofia Sans (fallback: system-ui, Segoe UI, Arial, sans-serif)

**Character:** Sofia Sans is made by Lettersoup, a Bulgarian foundry, and draws Bulgarian Cyrillic forms. They switch on through `lang="bg"` (OpenType `locl`).

- The extra-condensed width at weight 800 is the voice of the афиш: tall, tight, uppercase, filling the sheet.
- The normal width at 400–700 is calm and legible for forms, tables and prose.

Both fonts are variable (weight 1–1000) and self-hosted as woff2 in `wwwroot/fonts`. Only the cyrillic and latin subsets are shipped, with `font-display: swap`, and the OFL licence file sits beside them.

### Hierarchy

- **Display** (800, `clamp(2.75rem, 1.6rem + 5vw, 5.5rem)`, line-height 0.9, uppercase): the event title on its details page and the АФИШ masthead. Persuade surfaces only.
- **Poster title** (800, fitted, line-height 0.88, uppercase): sized to the poster's own width, see Components.
- **Headline** (800, 2.25rem, line-height 1, uppercase): page titles on Operate surfaces (Количка, Плащане, Табло) and section heads on Persuade surfaces, under a heavy rule.
- **Figure** (800, 3rem, line-height 0.85, tabular): date day numbers, the seat number on a ticket, KPI values.
- **Title** (Sofia Sans 700, 1.25rem, line-height 1.25): card and group titles, cart group headers.
- **Body** (400, 1rem, line-height 1.5, prose capped at 68ch): everything you read. The root stays 16px at every width; the old 14px mobile root is gone.
- **Label** (700, 0.8125rem, line-height 1.35, letter-spacing 0.05em, uppercase; inside posters, sentence case except the month): kickers, badges, table headers, legend items. This is the floor: no text is smaller than 13px.

Operate surfaces use the fixed rem steps above. Only Display and poster titles are fluid.

### Named Rules

**The Two Widths Rule.** Extra Condensed is for things you look at: posters, titles, dates and figures. Buttons, labels, inputs and table data are always normal-width Sofia Sans.

**The Fit Rule.** A poster title fills its poster's width. Its size comes from the longest word, never from a fixed step. Titles wrap with `text-wrap: balance` and are never truncated.

## Layout

- **Grid:** Bootstrap's `.container` and grid. Breakpoints are Bootstrap's: sm 576, md 768, lg 992, xl 1200 px.
- **Side gutter:** 16px at every width (`--bs-gutter-x: 2rem`; it was 10.5px on phones).
- **Spacing:** a 4px base, mostly `{spacing.md}` inside components and `{spacing.xl}` between page sections (32px on phones). Each heading has more space above it than below.
- **Афиш:** a poster wall with 2 columns below md, 3 at md and 4 at lg. Gaps are 16px (12px on phones).
  - Filters sit in one row above the wall. Type chips (Всички · Филми · Концерти · Театър) and the city are always visible. Genre, venue, date, sort and search fold into a "Още филтри" disclosure below md.
- **Event details:**
  - lg: the poster takes 4 columns, title and facts take 8, and the programme dates run full width below.
  - Below md: the poster becomes a full-bleed band (about 16:10) with the title. The dates follow at once, so "Избери места" is on screen without sideways scrolling.
- **Booking (seats, cart, checkout):**
  - lg: work area 8 columns, sticky summary 4 columns.
  - Below lg: a single column plus a sticky bottom bar (paper, 2px ink top rule, safe-area padding) holding the total and the one primary action.
- **Public tables become cards below md:** showtimes, cart lines and profile tickets. Admin tables stay tables inside `.table-responsive`.
- **Admin:** the same container, with a second-level tab bar under the main nav. It is dense: 8–12px table cell padding and no hero areas.

**The Poster First Rule.** On a 375 × 667 phone, the top of the first poster on the Афиш is visible without scrolling.

## Elevation & Depth

The system is flat. Depth comes from paper layers and printed rules: the sheet sits on paper, and section starts get a heavy 3px ink rule.

- Sticky bars and the open mobile menu separate with a 2px ink rule, not a shadow.
- Overlays (the navbar collapse, any dropdown) use a 1px ink border.
- The focus ring is an outline, not a glow.

**The Paper Rule.** Depth is paper on paper. If an element seems to need a shadow, give it a sheet background and a rule instead.

## Shapes

- **Corners:**
  - Posters, cards, sheets and tickets are square (`{rounded.none}`), like printed matter.
  - Controls (buttons, inputs, selects, chips, badges) get a barely softened `{rounded.sm}`.
  - Seats are the one exception: a chair back (`{rounded.seat}`).
- **Rules:**
  - Heavy 3px ink rules open sections and mark the stage edge.
  - A double rule (3px plus 1px, 3px apart) closes the theatre poster's title.
  - 1px hairlines divide table rows.
- **Hall fronts:**
  - Film has a shallow screen arc drawn with an inline SVG path.
  - Concert and theatre have a stage block: a rectangle in the event's ink.
- **Perforation:** tickets and the hold badge use a dashed 1px rule as a tear line between the main part and the stub.
- **Not used:** pills, circles, many-vertex `clip-path` shapes, side-stripe borders.

## Components

Each component keeps Bootstrap's markup and accessibility behaviour and changes only its skin.

### Buttons

- **Shape:** `{rounded.sm}`, at least 44px tall, label in Sofia Sans 600, sentence case.
- **Primary** (`button-primary`): ink fill with sheet text; it hovers to worn ink. There is one primary per region; in booking it is the action in the sticky bar or summary.
- **Secondary** (`button-secondary`): sheet fill, ink text, 1.5px ink border. Used for Печат, Назад, Изчисти, Приложи and Експорт CSV, which keeps every secondary button the same colour.
- **Tertiary:** an underlined ink text link, for in-flow navigation such as "Обратно към количката".
- **Destructive:**
  - In lists, a quiet danger-coloured text button. It is never a filled red block, and never ten of them stacked down a table.
  - The filled danger button appears only on the delete confirmation page.
  - Each destructive control names its object for screen readers, e.g. "Изтрий „Под игото“".
- **Focus:** `outline: 3px solid ink; outline-offset: 2px` on every focusable element, posters and seats included. It replaces Bootstrap's blue box-shadow.

### Type badges and filter chips

- **Type badge** (`badge-type`):
  - Ink label text with a 3px underline in the event's type ink (built so; a filled field failed on paper for saffron). Words: ФИЛМ, КОНЦЕРТ, ТЕАТЪР (the `EventType` display names).
  - It always carries the word.
  - It is 13px, never Bootstrap's 0.75em, which rendered at 10.5px.
- **Genre:** plain worn-ink text after a middle dot ("· Драма"), not a badge.
- **Filter chips** (Афиш type filter):
  - Links styled as `{rounded.sm}` chips. The active one carries `aria-current="page"`.
  - Unselected: an ink outline. Selected: an ink fill.
  - A click applies the type at once and keeps the filters already applied (GET link, back to page 1). They work without JavaScript. A hidden `Type` field keeps the type when the other filters apply.
- **Other Афиш filters** apply without a button: selects on change (a keyboard pick waits for Enter), search and date on Enter or when leaving the field (`data-autosubmit` in `site.js`). Without JavaScript a "Покажи" button appears inside `<noscript>`.

### Inputs and fields

- **Style:** sheet fill, 1px field-edge border, `{rounded.sm}`, 44px tall. Every input has a visible label; an `aria-label` is used only where the layout makes a visible label redundant.
- **Focus:** the ink outline ring. The border does not turn blue.
- **Error:**
  - danger border plus a danger message under the field
  - linked with `aria-describedby`
  - focus moves to the first error
- **Payment fields:**
  - autocomplete tokens `cc-name`, `cc-number`, `cc-exp`, `cc-csc`
  - `inputmode="numeric"` on the number, expiry and CVV fields
  - `additional-methods.min.js` loaded so the CreditCard rule works on the client

### Navigation

- **Bar:** paper, with a 3px ink rule below it.
  - The wordmark "EventTix Hub" is set in Extra Condensed 800.
  - Links are Sofia Sans 600. The current link has a 3px ink underline and `aria-current="page"`.
- **Right side:**
  - the hold badge, when a hold exists. It sits outside the mobile collapse, so it is always visible.
  - Количка
  - the theme toggle: an inline-SVG sun or moon with the label "Тъмна тема" / "Светла тема" and `aria-pressed`
  - the user name and Изход
- **Mobile:** the wordmark, the hold badge and the menu toggle stay visible; the rest collapses. A skip link ("Към съдържанието") comes first in the tab order.
- **Footer:** an ink rule, "EventTix Hub · билети за кино, концерти и театър · София · Пловдив · Варна", and "Учебен проект · плащането е симулирано".

### Афиш poster (signature)

A container (`container-type: inline-size`) at 3:4, filled with the event's ink. Label lines on the poster are sentence case (long uppercase lines were flagged by the detector); only the month under the day figure stays uppercase. All its text is real HTML text: the title is the card's link, made clickable across the whole card with a stretched link.

- **Fitted title:**
  - `font-size: min(var(--cap), calc(100cqi / (var(--longest) * 0.55)))`
  - Razor passes `--longest`, the character count of the longest word.
  - The 0.55 width factor is tuned once against the real font. Final (verified 2026-10-06, no drift from the seed): 0.55 holds for all seeded titles at 390px and 1280px, including "Симфоничен оркестър: Бетовен", with no overflow.

The three art forms get three typographic voices, not one template in three colours:

- **Театър, the classic афиш:**
  - centred stack
  - venue in label style, then a hairline
  - TITLE
  - genre in Sofia Sans italic
  - double rule
  - date line "9 ОКТОМВРИ · ЧЕТВЪРТЪК · 20:00"
- **Филм, the screening:**
  - flush left
  - kicker "ФИЛМ · ФАНТАСТИКА"
  - day figure "09" over "ОКТ" at top right
  - the same shallow screen arc as the hall plan
  - TITLE anchored bottom left
  - time and venue on the last line
- **Концерт, the gig bill:**
  - flush left, the heaviest set
  - TITLE stacked
  - day figure large
  - the bottom of the sheet is an ink stage bar carrying time, venue and city in saffron (9.6:1)

**Other poster states:**

- **No upcoming date:** the date area reads "БЕЗ ПРЕДСТОЯЩИ ДАТИ".
- **Uploaded image:** the image fills the poster with `object-fit: cover`. An ink band at the bottom carries the type word, the title and the date line, so the system survives any artwork.
- **Variants:** card (Афиш), hero (details, lg), band (details, below md), thumb (admin lists, 48px wide, title omitted).

### Programme dates (event details)

The heading uses the event-type plural: "Предстоящи прожекции" / "концерти" / "представления". Each date is one entry:

- the day figure, month and weekday ("09 · ОКТ · ЧТ")
- the time
- the venue and city
- prices on one line ("9,00 € · VIP 14,00 €")
- free seats ("47 свободни места")
- the primary "Избери места"

A sold-out date shows "Изчерпани" in label style instead of the button. Below md, each entry is a card and the button is full width.

### Hall plan (signature)

The seat map is drawn as the hall.

- **Front of the hall:**
  - Film: the screen arc in film ink, labelled ЕКРАН.
  - Concert and theatre: the stage block in the event's ink, labelled СЦЕНА.
  - The front spans exactly the seat rows and never widens the hall.
- **Phones:**
  - A hall wider than its frame opens scrolled once to its centre; later updates, selection and resizes never move it.
  - While the hall is wider than its frame, one muted line under the frame says "Превъртете настрани, за да видите цялата зала." It is hidden from assistive technology, follows viewport resizes, and the native scrollbar stays. No edge fades.
  - The connection and conflict notices sit between the legend and the frame, so they never fall under the fixed summary bar.
  - Below lg the page's bottom padding always equals the summary bar's current height.
- **Rows:** row numbers on both sides. VIP rows sit on the zone band, labelled "VIP · 14,00 €".
- **Seats:** `<button>`s with the chair-back corner. They are 36px (32px below md) with 6px gaps (4px below md). The hall scrolls sideways inside its frame instead of shrinking seats.
- **Seat states:**
  - **Free:** sheet fill (night-seat-free in dark), 1.5px worn-ink outline, ink number.
  - **Selected:** the event's ink fill with its paired number colour, a 3px ink border and `aria-pressed="true"`.
  - **Held:** a diagonal hatch in seat-held.
  - **Sold:** a solid seat-sold fill with a muted number.
  - Held and sold seats are `aria-disabled="true"`, not `disabled`, so they stay reachable and are announced.
- **Behaviour:**
  - The grid renders once and is then updated in place, keyed by seat id.
  - It is a single tab stop with a roving `tabindex`:
    - arrow keys move within and between rows
    - Home and End jump to the row ends
    - Enter or Space toggles a seat
  - The focused seat is never replaced.
  - Monitor mode (admin) reuses the plan read-only.
- **Summary:**
  - It lists the chosen seats ("Ред 5 · място 7 · VIP · 14,00 €"), each with its own remove button, then the total and the hold promise: "Ще пазим местата 10 минути, докато платите. До 10 билета в поръчка."
  - A polite visually-hidden region announces changes only ("Добавено: ред 5, място 7. Общо 28,00 €."). Conflicts keep their `role="alert"` notice that names the seat.
- **Legend:** shape swatches for свободно, избрано, резервирано and продадено, plus the VIP band.

### Hold badge (signature)

- **Calm state** (`hold-badge`): a ticket-stub chip with a clock glyph, a perforation and "Пазим местата ви още 7:42". It links to the cart.
- **Placement:** in the nav whenever the user has an active hold, and as a larger header on the cart and checkout pages.
- **Countdown:** it counts down from a server-rendered number of seconds, so client clock skew does not matter.
- **Under 2:00** (`hold-badge-urgent`): a danger field with bold text, switched once without animation.
- **Announcements:** a separate live region speaks only at 5:00, 1:00 and 0:00.
- **At expiry:**
  - The badge reads "Резервацията изтече".
  - The cart names the lapsed seats.
  - It offers "Опитай отново със същите места", linking to the seat page with those seats preselected when still free.

**The Promise Rule.** While a hold exists, its remaining time is on screen on every page of the booking path.

### Invitation and ticket (signature)

The confirmation is a покана (invitation), not a receipt.

- **Invitation block** (one per showtime in the order):
  - kicker "Платено · поръчка №12"
  - "Очакваме ви на прожекцията" / "концерта" / "представлението"
  - the event title in Display
  - date, time, venue, address and city
- **Ticket cards:**
  - square sheets with a perforated stub
  - the seat as a Figure ("Ред 5 · Място 7")
  - zone, concession, price
  - the QR code as the centrepiece: at least 200px, on a white quiet zone in both themes
  - the code `ETH-…` in tabular figures that never break
- **Print:** each ticket avoids page breaks, the page is light, and nav and footer are hidden.

### Tables and occupancy bars (admin)

- **Tables:** label-style headers, hairline rows, numbers right-aligned in tabular figures. They always sit inside `.table-responsive`.
- **Type column:** shows the type badge.
- **Row actions:** Редакция and Места/Детайли as text links, plus the quiet destructive button last.
- **Programme board (dashboard):**
  - Each upcoming showtime shows a day figure, the title with its type badge, the venue, and an occupancy bar filled with the event's ink on a hairline track.
  - The percentage and "59 от 140" sit outside the bar, never clipped inside it.
  - At 90 % or more, a "критична" label appears in label style. Red is not used, because a near sell-out is good news.
  - Each bar has an `aria-label`.

### Alerts and status pages

- **Alerts:** a sheet with a 1px border in the semantic colour and a bold lead word ("Грешка:", "Готово:"). No left stripe, and dismissible as before.
- **404, 403 and 500 pages:** a kicker ("Грешка 404"), a Display headline ("Страницата не е намерена"), one sentence, and "Към афиша".

### Motion

- State changes (seat fill, button hover, chip select) take 150–200 ms ease-out.
- There is no page-load choreography.
- The hold badge's urgent switch is instant.
- Under `prefers-reduced-motion: reduce`, every transition is removed.

## Do's and Don'ts

### Do:

- **Do** set poster titles in Sofia Sans Extra Condensed 800 uppercase, fitted to the poster width (The Fit Rule).
- **Do** pair every type ink with its text: `{colors.on-film}` on film, `{colors.on-theatre}` on theatre, ink on concert (The Field Rule).
- **Do** print the type word (ФИЛМ, КОНЦЕРТ, ТЕАТЪР) on every poster and badge. Colour is never the only cue.
- **Do** follow the event type in wording and hall front: прожекция / концерт / представление, ЕКРАН / СЦЕНА.
- **Do** format money as `12,50 €` with a non-breaking space, on one line, in tabular figures in lists.
- **Do** keep the root font at 16px. 13px is the smallest text anywhere.
- **Do** keep the QR code on a white quiet zone in both themes, and print in light.
- **Do** self-host every font file in `wwwroot/fonts` with its OFL licence.

### Don't:

- **Don't** use shadows, gradients, glass, glows or fake paper or grain textures (The Paper Rule).
- **Don't** colour text with a type ink on paper, or use the three inks for anything but their art form (The Three Inks Rule).
- **Don't** use Bootstrap's `#0d6efd` or its blue focus glow (The Ink Rule).
- **Don't** use the display width for buttons, labels, inputs or table data (The Two Widths Rule).
- **Don't** render a grey "Без плакат" box. Every event gets a generated poster.
- **Don't** imitate real film or concert artwork, artist logos or likenesses in generated posters.
- **Don't** use pills, side-stripe borders, emoji icons or a hosted font stylesheet.
- **Don't** shrink seats below 32px. Scroll the hall instead.
