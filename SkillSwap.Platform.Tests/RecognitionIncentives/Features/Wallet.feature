Feature: SkillCredits wallet
  As a verifier
  I want to earn, consult and redeem SkillCredits
  So that my work of verifying is recognized

  Background:
    Given a signed-in student "ana"
    And a signed-in student "bob"
    And "ana" has declared the goal "quiero aprender a construir APIs REST con autenticación JWT"
    And "bob" has declared the goal "quiero aprender a construir APIs REST con autenticación JWT"
    And "bob" has completed the skill "networking-basics"
    And "bob" is a verifier of the skill "networking-basics"

  @US30
  Scenario: Resolving a case credits the verifier
    Given "ana" has failed the assessment of the skill "networking-basics"
    When "bob" resolves the case of "ana" as "Approved" with the notes "Solid understanding of the network layers."
    Then the response status is 200
    And the wallet of "bob" shows a balance of 10

  @US30
  Scenario: A rejected case also credits the verifier
    Given "ana" has failed the assessment of the skill "networking-basics"
    When "bob" resolves the case of "ana" as "Rejected" with the notes "The explanation of the layers is incomplete."
    Then the response status is 200
    And the wallet of "bob" shows a balance of 10

  @US30
  Scenario: The student of the case is not credited
    Given "ana" has failed the assessment of the skill "networking-basics"
    When "bob" resolves the case of "ana" as "Approved" with the notes "Solid understanding of the network layers."
    Then the wallet of "ana" shows a balance of 0

  @US31
  Scenario: A new account has an empty wallet
    When "ana" consults the wallet of "ana"
    Then the response status is 200
    And the response shows a balance of 0

  @US31
  Scenario: The history lists the movements from the most recent
    Given "bob" has earned 30 SkillCredits by resolving cases
    And "bob" has redeemed "ContributionCertificate"
    When "bob" consults the wallet history of "bob"
    Then the response status is 200
    And the history lists 4 movements and the most recent is "Redeemed" of 30 SkillCredits

  @US31
  Scenario: The wallet of another user is private
    When "ana" consults the wallet of "bob"
    Then the response status is 403
    And the error is "NotWalletOwner"

  @US32
  Scenario: Redeem a benefit with enough balance
    Given "bob" has earned 30 SkillCredits by resolving cases
    When "bob" redeems "ContributionCertificate"
    Then the response status is 201
    And the movement is recorded as "Redeemed" of 30 SkillCredits
    And the wallet of "bob" shows a balance of 0

  @US32
  Scenario: Reject a redemption with insufficient balance
    Given "bob" has earned 20 SkillCredits by resolving cases
    When "bob" redeems "ContributionCertificate"
    Then the response status is 409
    And the error is "InsufficientBalance"
    And the wallet of "bob" shows a balance of 20

  @US32
  Scenario: The advanced path unlock costs fifty credits
    Given "bob" has earned 40 SkillCredits by resolving cases
    When "bob" redeems "AdvancedPathUnlock"
    Then the response status is 409
    And the error is "InsufficientBalance"

  @US32
  Scenario: A student without credits cannot redeem
    When "ana" redeems "ContributionCertificate"
    Then the response status is 409
    And the error is "InsufficientBalance"

  @US32
  Scenario: Reject an unknown benefit
    When "ana" redeems "coffee"
    Then the response status is 400
    And the error is "InvalidRedemptionItem"