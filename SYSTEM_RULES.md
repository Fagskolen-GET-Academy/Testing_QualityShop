# Quality Shop system rules

This file is the authoritative description of expected business behavior. Test results and observed application behavior should be compared with these rules.

## Products and stock

A product has an ID, name, price, and stock quantity. The initial product set is:

| ID | Product | Price (NOK) |
| ---: | --- | ---: |
| 1 | Mechanical Keyboard | 899.00 |
| 2 | Wireless Mouse | 399.00 |
| 3 | USB-C Hub | 499.00 |
| 4 | Webcam | 749.00 |
| 5 | Laptop Stand | 599.00 |
| 6 | Headset | 999.00 |

A cart operation must refer to an existing product. Quantity must be at least 1 and cannot exceed current stock. Adding a product that is already in the cart increases its existing quantity; the combined quantity must still not exceed stock.

Stock is reduced only when an order succeeds. Reset restores products, stock, cart, and orders to their seed state.

## Cart and subtotal

The application has one shared cart (in memory in the baseline and memory variants, persisted in the SQL Server variant). Each line contains product ID, product name, unit price, quantity, and line total.

```text
Line total = unit price × quantity
Subtotal = sum of all line totals
```

Removing all items leaves an empty cart. Clearing the cart also removes its discount code.

## Discount

The only supported code is `SAVE10`. The code is case-insensitive, so `SAVE10`, `save10`, and `Save10` have identical meaning.

- A valid code gives a 10% discount on the subtotal.
- Discount is calculated before shipping.
- An unknown code is rejected.
- An empty code means no discount.
- Discount cannot make the item value negative.

Example:

```text
Subtotal: 1200.00
Discount (10%): 120.00
Cart value after discount: 1080.00
```

## Shipping and total

Shipping is based on item value after discount:

- At least NOK 1000.00: free shipping.
- Below NOK 1000.00: NOK 99.00 shipping.

The exact boundary matters:

```text
Cart value after discount: 999.99
Shipping: 99.00
```

```text
Cart value after discount: 1000.00
Shipping: 0.00
```

```text
Total = cart value after discount + shipping
```

Total can never be negative.

## Orders

An order can be created only when:

- the cart contains at least one item;
- every product still has enough stock for the cart quantity; and
- any discount code is valid.

On success, the system generates an order ID and stores creation time, order lines, subtotal, discount, shipping, and total. It then reduces product stock and empties the cart.

If validation or stock checking fails, no order is created, stock is unchanged, and the cart remains available for correction.
