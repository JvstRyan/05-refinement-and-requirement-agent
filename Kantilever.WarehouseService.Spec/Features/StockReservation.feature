Feature: Stock reservation
    As a sales system
    I want to reserve stock for orders
    So that inventory is correctly allocated before orders are fulfilled

Background:
    Given the warehouse system is available
    And the following articles exist in the warehouse:
        | ArticleNumber | Stock | Reserved |
        | 2001          | 100   | 20       |
        | 2002          | 30    | 5        |
        | 2003          | 15    | 15       |
        | 2004          | 0     | 0        |

Scenario: Reserve stock for article with sufficient available stock
    Given article 2001 has 100 units in stock with 20 reserved
    When I reserve 25 units for article 2001
    Then the stock for article 2001 is 100 units
    And the reserved stock for article 2001 is 45 units
    And the response status is 200
    And a "StockReserved" event is published

Scenario: Attempt to reserve more stock than available
    Given article 2002 has 30 units in stock with 5 reserved
    When I try to reserve 30 units for article 2002
    Then the response status is 409
    And the error message indicates "Insufficient stock available for reservation"
    And the reserved stock for article 2002 remains 5 units

Scenario: Attempt to reserve stock when all stock is already reserved
    Given article 2003 has 15 units in stock with 15 reserved
    When I try to reserve 1 unit for article 2003
    Then the response status is 409
    And the error message indicates "Insufficient stock available for reservation"

Scenario: Attempt to reserve stock for non-existing article
    When I try to reserve 10 units for article 9999
    Then the response status is 404
    And the error message indicates "Article not found"

Scenario: Attempt to reserve negative stock
    When I try to reserve -5 units for article 2001
    Then the response status is 422
    And the error message indicates "Quantity cannot be negative"

Scenario: Attempt to reserve stock for article with zero stock
    Given article 2004 has 0 units in stock with 0 reserved
    When I try to reserve 5 units for article 2004
    Then the response status is 409
    And the error message indicates "Insufficient stock available for reservation"

Scenario: Reserve exact amount of available stock
    Given article 2002 has 30 units in stock with 5 reserved
    When I reserve 25 units for article 2002
    Then the stock for article 2002 is 30 units
    And the reserved stock for article 2002 is 30 units
    And the response status is 200
    And a "StockReserved" event is published
