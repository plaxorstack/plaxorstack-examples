# Contributing

Thanks for helping. Fixes to a walkthrough, a client in another language, or a new example are all
welcome.

## Before you open a pull request

- **No keys, ever.** Use `YOUR_API_TOKEN` / `YOUR_WIDGET_KEY` as placeholders and read real values
  from `PLAXOR_API_KEY` and `PLAXOR_MODEL_ID`. CI fails a pull request that looks like it contains
  a key.
- **Run what you changed.** Train the example on the free trial and run every client you touched
  against your model. Put the real output in the README, not what you expect it to print.
- **Keep the four clients in step.** `predict.sh`, `predict.py`, `predict.mjs` and `predict.cs`
  send the same request and print the same line. A change to one is a change to all four.
- **No dependencies beyond the standard library**, except `requests` for Python. An example should
  run on a fresh machine.

## A new example

Copy the layout of an existing one:

```
<area>/<task-in-plain-words>/
├── README.md        the story, the data, upload → train → try → call → make it yours
├── data/            the file(s) to upload, small enough for the free trial
└── api/             predict.sh · predict.py · predict.mjs · predict.cs
```

- **Name it after the job it does** (`route-support-tickets`), not the algorithm.
- **Data must be redistributable.** Add a row to [DATA-LICENSES.md](DATA-LICENSES.md) with the
  source, the licence and every change you made. If you generate data, add a builder to
  `tools/DatasetBuilder` with a fixed seed, so anyone can rebuild the same file.
- **Say what to expect.** Give the metric a reader should see, a table of inputs to try in the
  browser with the right answers, and what changes on real data.
- Add a row to the table in the root [README.md](README.md).

## Checks

CI runs on every pull request: it builds every C# client, checks the shell, Python and Node
syntax, looks for anything resembling a key, and checks the links in every README. To run the main
ones locally:

```sh
for f in $(find . -name '*.sh' -not -path '*/node_modules/*'); do sh -n "$f"; done
python -m py_compile $(find . -name '*.py')
for f in $(find . -name '*.mjs' -not -path '*/node_modules/*'); do node --check "$f"; done
for f in $(find . -name 'predict.cs'); do dotnet build "$f"; done
```

## Reporting a problem

Open an issue with the example's name, the step that went wrong and what you saw. For anything
security related, see [SECURITY.md](SECURITY.md) instead.
