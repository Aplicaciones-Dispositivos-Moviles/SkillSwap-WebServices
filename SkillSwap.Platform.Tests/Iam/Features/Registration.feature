@US01
Feature: Registration with an institutional email
  As a student
  I want to register with my institutional email
  So that I can access the platform as a university user

  Scenario: Register with a valid institutional email
    When I sign up as "ana" with the email "ana@upc.edu.pe" and the password "password123"
    Then the response status is 201
    And the created account has the role "Student"

  Scenario Outline: Reject a registration with a non-institutional email
    When I sign up as "ana" with the email "<email>" and the password "password123"
    Then the response status is 400
    And the error is "InvalidInstitutionalEmail"

    Examples:
      | email         |
      | ana@gmail.com |
      | ana@upc.edu   |
      | ana@edu.pe    |

  Scenario: Reject a registration with an email already in use
    Given an account exists for "ana" with the email "ana@upc.edu.pe"
    When I sign up as "bob" with the email "ana@upc.edu.pe" and the password "password123"
    Then the response status is 409
    And the error is "EmailAlreadyTaken"

  Scenario: Reject a registration with a username already in use
    Given an account exists for "ana"
    When I sign up as "ana" with the email "another@upc.edu.pe" and the password "password123"
    Then the response status is 409
    And the error is "UsernameAlreadyTaken"

  Scenario: Reject a registration with a weak password
    When I sign up as "ana" with the email "ana@upc.edu.pe" and the password "short"
    Then the response status is 400
    And the error is "WeakPassword"

  Scenario: A client cannot choose its own role
    When I sign up as "mallory" with the email "mallory@upc.edu.pe", the password "password123" and the role "Coordinator"
    Then the response status is 201
    And the created account has the role "Student"