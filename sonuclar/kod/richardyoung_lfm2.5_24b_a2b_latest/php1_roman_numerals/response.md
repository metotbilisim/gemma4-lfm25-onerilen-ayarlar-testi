

```php
<?php
final class RomanNumerals {
    private static array $valueMap = [
        1000 => 'M',
        900  => 'CM',
        500  => 'D',
        400  => 'CD',
        100  => 'C',
        90   => 'XC',
        50   => 'L',
        40   => 'XL',
        10   => 'X',
        9    => 'IX',
        5    => 'V',
        4    => 'IV',
        1    => 'I',
    ];

    public static function toRoman(int $n): string {
        if ($n < 1 || $n > 3999) {
            throw new \InvalidArgumentException('Number must be between 1 and 3999');
        }
        $result = '';
        foreach (self::$valueMap as $value => $symbol) {
            while ($n >= $value) {
                $result .= $symbol;
                $n -= $value;
            }
        }
        return $result;
    }

    private static function romanValue(string $s): int {
        $map = [
            'I' => 1,
            'V' => 5,
            'X' => 10,
            'L' => 50,
            'C' => 100,
            'D' => 500,
            'M' => 1000,
        ];
        $total = 0;
        $len = strlen($s);
        for ($i = 0; $i < $len; $i++) {
            $current = $map[$s[$i]] ?? null;
            if ($current === null) {
                // Should not happen because we validate characters beforehand
                return 0;
            }
            if ($i + 1 < $len && isset($map[$s[$i + 1]]) && $map[$s[$i + 1]] > $current) {
                $total -= $current;
            } else {
                $total += $current;
            }
        }
        return $total;
    }

    public static function fromRoman(string $s): int {
        if ($s === '') {
            throw new \InvalidArgumentException('Empty string is not a valid Roman numeral');
        }
        // Only uppercase letters allowed
        if (!preg_match('/^[IVXLCDM]+$/', $s)) {
            throw new \InvalidArgumentException('Invalid characters in Roman numeral');
        }
        $value = self::romanValue($s);
        // Verify canonical form: toRoman must produce the same string
        $canonical = self::toRoman($value);
        if ($canonical !== $s) {
            throw new \InvalidArgumentException('Non-canonical Roman numeral');
        }
        return $value;
    }

    public static function add(string $a, string $b): string {
        try {
            $valA = self::fromRoman($a);
            $valB = self::fromRoman($b);
        } catch (\InvalidArgumentException $e) {
            throw $e;
        }
        $sum = $valA + $valB;
        if ($sum < 1 || $sum > 3999) {
            throw new \InvalidArgumentException('Sum out of range');
        }
        return self::toRoman($sum);
    }
}
```