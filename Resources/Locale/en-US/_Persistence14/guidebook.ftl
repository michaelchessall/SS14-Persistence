## Small and Simple Guidebook Entires for Persistence14 ##

# Polymorph #
entity-effect-guidebook-revert-polymorph = Reverts the target to its original form
entity-effect-guidebook-random-polymorph = Polymorphs the target into a random creature

# Conditions #
entity-condition-has-component = 
{ $inverted ->
    [true] the target does not have the { $component } component
    *[false] the target has the { $component } component
}