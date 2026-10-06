---
version: 1
slug: "eventtixhub-web-views-booking-seats-cshtml"
primary_target: "EventTixHub.Web/Views/Booking/Seats.cshtml"
related_targets: ["EventTixHub.Web/Views/Booking","EventTixHub.Web/Views/Tickets","EventTixHub.Web/Views/Profile"]
---

## Direction contract

Scope: booking path, seats, cart, checkout, confirmation, ticket, profile (Operate). Logged-in buyer picks exact seats and pays a simulated card.

THESIS: Booking happens in the hall, not in a form. The seat map is drawn as the real room with its screen or stage in the event's ink, and the hold stays a visible promise until payment. It refuses a grid of buttons followed by a table checkout.

OWN-WORLD: The same tokens. Signature moves: the hall plan (front shape, VIP zone band, chair-back seats), the hold badge (a ticket stub, calm until urgent under 2:00), the invitation ticket with the QR code as the centrepiece.

STORY: The buyer picks seats by mouse, touch or keyboard and always sees which seats and what they cost, reads the hold rule before committing, sees the time left through cart and checkout, then pays and receives an invitation to show at the door.

FIRST VIEWPORT (seats): An event band (type badge, title, date and time, venue); the hall with its front at the top; a summary listing chosen seats, total, the hold rule and "Добави в количката": sticky right panel at lg, sticky bottom bar on phones.

FORM: user-pinned (no roll; seed key: none).

FINISH: unreviewed and undocumented is unfinished; this build ends with the finish review, the verdict, DESIGN.md, and every shipping raster carrying its provenance
