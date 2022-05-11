# HigherLogics.Locale

This project is intended to provide general business functionality, like:

 * mailing address parsing and validation
 * a currency-typed Money abstraction

The current project name is a bit of a misnomer. It should probably be something
more like HigherLogics.Sales or HigherLogics.Enterprise.

# Future Work

 * maybe make country, currency, continents, and their associations dynamically loaded
   so we don't need to rebuild for any changes?
 * Fixed<T> should reference HigherLogics.Algebra
 * An invoice type that abstracts over an invoice via
   Func<Invoice, Money, LineItem>, which can provide context
 * exchange rate conversions (online? offline? manual?)
 * BillOfMaterials?
 * extensions for mailing address validation to verify it via an online source?