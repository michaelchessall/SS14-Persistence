tray-component-nutrient-message = {CAPITALIZE($name)}: [color={$color}]{$nutrientLevel}[/color].

tray-component-nutrient-requirement-message = /{ $fulfilled ->
        [true] [color=green]{$requiredNutrients}[/color]
       *[false] [color=red]{$requiredNutrients}[/color]
    }

tray-component-nutrient-bonus-message = -{ $fulfilled ->
        [true] [color=green]{$bonusNutrients}[/color]
       *[false] {$bonusNutrients}
    }
