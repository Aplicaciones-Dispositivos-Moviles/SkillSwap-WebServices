@US16
Feature: Certificate verification status
  As a student
  I want to check the verification status of my certificates
  So that I know which of them count as evidence

  Background:
    Given a signed-in student "ana"
    And a signed-in student "bob"

  Scenario: A student lists their certificates
    Given "ana" has uploaded the certificate file "file-1"
    And "ana" has uploaded the certificate file "file-2"
    And "bob" has uploaded the certificate file "file-3"
    When "ana" lists the certificates of "ana"
    Then the response status is 200
    And the response contains 2 certificates

  Scenario: A student opens one of their certificates
    Given "ana" has uploaded the certificate file "file-1"
    When "ana" opens the last certificate of "ana"
    Then the response status is 200
    And the certificate status is "Unverified"

  Scenario: A student cannot list another student's certificates
    Given "ana" has uploaded the certificate file "file-1"
    When "bob" lists the certificates of "ana"
    Then the response status is 403
    And the error is "NotCertificateOwner"

  Scenario: A student cannot open another student's certificate
    Given "ana" has uploaded the certificate file "file-1"
    When "bob" opens the last certificate of "ana"
    Then the response status is 403
    And the error is "NotCertificateOwner"