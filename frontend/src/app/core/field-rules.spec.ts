import { checkField, emailRequirements, nameRequirements, passwordRequirements } from './field-rules';

describe('checkField', () => {
  it('cleans an email and rejects a bad one', () => {
    expect(checkField('email', '  Ada@Example.COM ').value).toBe('ada@example.com');
    expect(checkField('email', 'not-an-email').ok).toBe(false);
  });

  it('strips markup from a name and enforces the length', () => {
    expect(checkField('name', '  <b>Ada</b>  ')).toEqual({
      ok: true,
      value: 'Ada',
      message: '',
    });
    expect(checkField('name', 'A').message).toBe('Enter your name.');
    expect(checkField('name', 'A'.repeat(81)).message).toBe('Use 80 characters or fewer.');
  });

  it('does not rewrite a password and rejects a short one', () => {
    expect(checkField('password', 'password1').ok).toBe(true);
    expect(checkField('password', 'abc').message).toBe('Use at least 8 characters.');
    expect(checkField('password', 'password 1').ok).toBe(false);
  });

  it('ticks email rules off as the address becomes valid', () => {
    const empty = emailRequirements('');
    expect(empty.every((rule) => !rule.met)).toBe(true);

    const partial = emailRequirements('ada');
    expect(partial.find((rule) => rule.id === 'email-min')?.met).toBe(false);
    expect(partial.find((rule) => rule.id === 'email-max')?.met).toBe(true);

    const done = emailRequirements('ada@example.com');
    expect(done.every((rule) => rule.met)).toBe(true);
  });

  it('ticks name and password rules off one at a time', () => {
    expect(nameRequirements('A').find((rule) => rule.id === 'name-min')?.met).toBe(false);
    expect(nameRequirements('Ada').every((rule) => rule.met)).toBe(true);

    const short = passwordRequirements('abc');
    expect(short.find((rule) => rule.id === 'password-letter')?.met).toBe(true);
    expect(short.find((rule) => rule.id === 'password-number')?.met).toBe(false);
    expect(short.find((rule) => rule.id === 'password-min')?.met).toBe(false);
    expect(passwordRequirements('password 1').find((rule) => rule.id === 'password-chars')?.met).toBe(
      false,
    );
    expect(passwordRequirements('password1').every((rule) => rule.met)).toBe(true);
  });

  it('stores a cleaned task title and rejects a script tag', () => {
    expect(checkField('title', '  Buy <b>milk</b>  ').value).toBe('Buy milk');
    expect(checkField('title', '<script>alert(1)</script>').ok).toBe(false);
    expect(checkField('description', '  <i>From the shop</i>  ').value).toBe('From the shop');
    expect(checkField('description', 'A').message).toBe('Write a description first.');
  });
});
