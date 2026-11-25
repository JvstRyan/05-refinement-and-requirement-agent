Feature: Stock management
    As a warehouse manager
    I want to manage article stock
    So that I can accurately track inventory

Background:
    Given the warehouse system is available
    And the following articles exist in the warehouse:
        | ArticleNumber | Stock | Reserved |
        | 1001          | 100   | 10       |
        | 1002          | 50    | 0        |
        | 1003          | 0     | 0        |

Scenario: Retrieve stock information for existing article
    When I request the stock for articleNumber 1001
    Then the response contains the stock information:
        | ArticleNumber | Stock | Reserved |
        | 1001          | 100   | 10       |
    And the response status is 200

Scenario: Retrieve stock information for non-existing article
    When I request the stock for articleNumber 9999
    Then the response status is 404
    And the error message indicates "Article not found"

Scenario: Add stock to existing article
    Given article 1002 has 50 units in stock
    When I add 25 units to article 1002
    Then the stock for article 1002 is 75 units
    And the response status is 200
    And a "StockPlacedInWarehouse" event is published

Scenario: Add stock to new article
    When I add 100 units to article 2001
    Then the stock for article 2001 is 100 units
    And the response status is 200
    And a "StockPlacedInWarehouse" event is published

Scenario: Attempt to add negative stock
    When I try to add -10 units to article 1001
    Then the response status is 422
    And the error message indicates "Quantity cannot be negative"

Scenario: Remove stock from article with sufficient stock
    Given article 1001 has 100 units in stock with 10 reserved
    When I remove 30 units from article 1001
    Then the stock for article 1001 is 70 units
    And the response status is 200
    And a "StockRetrievedFromWarehouse" event is published

Scenario: Attempt to remove more stock than available
    Given article 1002 has 50 units in stock with 0 reserved
    When I try to remove 60 units from article 1002
    Then the response status is 409
    And the error message indicates "Insufficient stock available"

Scenario: Attempt to remove stock from non-existing article
    When I try to remove 10 units from article 9999
    Then the response status is 404
    And the error message indicates "Article not found"

Scenario: Attempt to remove negative stock
    When I try to remove -5 units from article 1001
    Then the response status is 422
    And the error message indicates "Quantity cannot be negative"
