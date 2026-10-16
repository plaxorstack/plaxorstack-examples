# Security

## Reporting a vulnerability

If you find a security problem **in PlaxorStack itself** (the website, the API, the widget) or in
this repository, email **info@plaxorstack.com** with "Security" in the subject. Please don't open a
public issue.

Tell us what you found, how to reproduce it, and what someone could do with it. We'll reply within
three working days and keep you informed until it's fixed. We won't take action against anyone who
reports in good faith, doesn't access other customers' data beyond what's needed to show the
problem, and gives us reasonable time to fix it before disclosing.

## If you leaked a key

API keys are secret. If one ends up in a commit, a screenshot, a log or a web page:

1. **Delete it** at **Secrets** in the app. That stops it immediately.
2. Create a new one and update your server's environment variable.
3. Check usage on the AI Board for predictions you didn't make.

Removing the key from your git history doesn't help: assume anything that was pushed has been
copied. Deleting the key is the only fix.

Widget keys are designed to be public, but they still belong to you. Before going live, list your
site under *Allowed sites* so the key works nowhere else.

## In these examples

- No file here contains a real key. Every script reads `PLAXOR_API_KEY` from the environment, and
  widget pages contain the placeholder `YOUR_WIDGET_KEY`.
- `.env` is in `.gitignore`. Keep it that way.
- To call a model from a web page with your own UI, see [server-proxy](server-proxy/README.md):
  the browser talks to your server, and only your server holds the key.
