@US12
Feature: Upload a certificate from a file
  As a student
  I want to upload my certificate from a file
  So that it is stored and registered for verification

  Background:
    Given a signed-in student "ana"

  Scenario Outline: Upload a file in an accepted format
    When "ana" uploads a <format> certificate file
    Then the response status is 201
    And the certificate status is "Unverified"

    Examples:
      | format |
      | JPEG   |
      | PNG    |
      | PDF    |

  Scenario: Reject a file in a format that is not allowed
    When "ana" uploads a text certificate file
    Then the response status is 415
    And the error is "InvalidFileType"
    And the error message is "Only JPG, PNG and PDF files are accepted."

  Scenario: Reject a file larger than 10 MB
    When "ana" uploads a certificate file larger than 10 MB
    Then the response status is 413
    And the error is "FileTooLarge"

  Scenario: Reject a request without a file
    When "ana" uploads a request without a file
    Then the response status is 400
    And the error is "FileRequired"