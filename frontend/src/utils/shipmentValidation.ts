
export interface AddressSection {
  fullName: string;
  phone: string;
  street: string;
  city: string;
  state: string;
  postalCode: string;
  country: string;
}

export interface PackageSection {
  weightKg: string;
  lengthCm: string;
  widthCm: string;
  heightCm: string;
  description: string;
}

export type AddressErrors = Partial<Record<keyof AddressSection, string>>;
export type PackageErrors = Partial<Record<keyof PackageSection, string>>;

export function validateAddressSection(
  data: AddressSection,
  _label: 'Sender' | 'Receiver' = 'Sender'
): AddressErrors {
  const errors: AddressErrors = {};

  if (!data.fullName || data.fullName.trim().length < 3)
    errors.fullName = 'Name must be at least 3 characters';
  else if (/\d/.test(data.fullName))
    errors.fullName = 'Name must not contain numbers';

  if (!data.phone)
    errors.phone = 'Phone number is required';
  else if (data.country === 'India' && !/^[6-9]\d{9}$/.test(data.phone.replace(/\s/g, '')))
    errors.phone = 'Enter a valid 10-digit Indian mobile number (starts with 6-9)';
  else if (data.country !== 'India' && !/^\+?\d{7,15}$/.test(data.phone.replace(/\s/g, '')))
    errors.phone = 'Enter a valid international phone number';

  if (!data.street || data.street.trim().length < 10)
    errors.street = 'Street address must be at least 10 characters';

  if (!data.city || data.city.trim().length < 2)
    errors.city = 'Enter a valid city name';
  else if (/\d/.test(data.city))
    errors.city = 'City name must not contain numbers';

  if (!data.state || data.state.trim().length < 2)
    errors.state = 'Enter a valid state';

  if (!data.postalCode)
    errors.postalCode = 'Postal code is required';
  else if (data.country === 'India' && !/^\d{6}$/.test(data.postalCode))
    errors.postalCode = 'Enter a valid 6-digit PIN code';
  else if (data.country !== 'India' && data.postalCode.trim().length < 3)
    errors.postalCode = 'Enter a valid postal code';

  if (!data.country)
    errors.country = 'Country is required';

  return errors;
}

export function validatePackageSection(data: PackageSection): PackageErrors {
  const errors: PackageErrors = {};

  if (!data.weightKg || isNaN(Number(data.weightKg)) || Number(data.weightKg) <= 0)
    errors.weightKg = 'Enter a valid weight greater than 0';
  else if (Number(data.weightKg) > 1000)
    errors.weightKg = 'Weight cannot exceed 1000 kg';

  if (!data.lengthCm || isNaN(Number(data.lengthCm)) || Number(data.lengthCm) <= 0)
    errors.lengthCm = 'Enter a valid length';

  if (!data.widthCm || isNaN(Number(data.widthCm)) || Number(data.widthCm) <= 0)
    errors.widthCm = 'Enter a valid width';

  if (!data.heightCm || isNaN(Number(data.heightCm)) || Number(data.heightCm) <= 0)
    errors.heightCm = 'Enter a valid height';

  if (!data.description || data.description.trim().length < 3)
    errors.description = 'Description must be at least 3 characters';

  return errors;
}

export const hasErrors = (errors: Record<string, string | undefined>): boolean =>
  Object.values(errors).some(Boolean);